using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.Services
{
    public class DataConcentratorService : IDataConcentratorService
    {
        private readonly IPlcSimulator _plcSimulator;
        private readonly ITagValidationService _validationService;
        private readonly ISystemLogger _logger;
        private readonly IScadaRepository _repository;
        private readonly List<TagBase> _tags = new List<TagBase>();
        private readonly List<Alarm> _alarms = new List<Alarm>();
        private readonly List<ActivatedAlarm> _activatedAlarms = new List<ActivatedAlarm>();
        private readonly Dictionary<int, DateTime> _lastScanByTagId = new Dictionary<int, DateTime>();
        private int _nextTagId;
        private int _nextAlarmId;
        private int _nextActivatedAlarmId;

        public DataConcentratorService(
            IPlcSimulator plcSimulator,
            ITagValidationService validationService,
            ISystemLogger logger,
            IScadaRepository repository)
        {
            _plcSimulator = plcSimulator;
            _validationService = validationService;
            _logger = logger;
            _repository = repository;
        }

        public event EventHandler<TagValueChangedEventArgs> TagValueChanged;
        public event EventHandler<AlarmRaisedEventArgs> AlarmRaised;

        public IReadOnlyCollection<TagBase> Tags
        {
            get { return _tags.AsReadOnly(); }
        }

        public IReadOnlyCollection<Alarm> Alarms
        {
            get { return _alarms.AsReadOnly(); }
        }

        public IReadOnlyCollection<ActivatedAlarm> ActivatedAlarms
        {
            get { return _activatedAlarms.AsReadOnly(); }
        }

        public void LoadFromRepository()
        {
            var data = _repository.LoadAll();
            _tags.Clear();
            _alarms.Clear();
            _activatedAlarms.Clear();
            _lastScanByTagId.Clear();

            _tags.AddRange(data.Tags);
            _alarms.AddRange(data.Alarms);
            _activatedAlarms.AddRange(data.ActivatedAlarms);
            _nextTagId = data.NextTagId;
            _nextAlarmId = data.NextAlarmId;
            _nextActivatedAlarmId = data.NextActivatedAlarmId;

            foreach (var tag in _tags)
            {
                RegisterTagWithPlc(tag);
            }

            _logger.Log(string.Format("Učitano {0} tagova, {1} alarma i {2} aktiviranih alarma iz baze.", _tags.Count, _alarms.Count, _activatedAlarms.Count));
        }

        public void AddTag(TagBase tag)
        {
            var validationErrors = _validationService.ValidateTag(tag, _tags).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            tag.Id = ++_nextTagId;
            tag.LastUpdated = DateTime.Now;
            RegisterTagWithPlc(tag);

            _tags.Add(tag);
            _repository.SaveTag(tag);
            _logger.Log(string.Format("Dodat tag '{0}' tipa {1}.", tag.TagName, tag.TagType));
        }

        public void UpdateTag(TagBase tag)
        {
            var existingTag = _tags.FirstOrDefault(t => t.Id == tag.Id);
            if (existingTag == null)
            {
                throw new InvalidOperationException("Tag za izmenu nije pronađen.");
            }

            var validationErrors = _validationService.ValidateTag(tag, _tags).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            var index = _tags.IndexOf(existingTag);
            tag.CurrentValue = existingTag.CurrentValue;
            tag.LastUpdated = existingTag.LastUpdated;
            tag.IsInAlarm = existingTag.IsInAlarm;
            tag.HasUnacknowledgedAlarm = existingTag.HasUnacknowledgedAlarm;

            var analogInputTag = tag as AnalogInputTag;
            if (analogInputTag != null)
            {
                var previousAlarms = (existingTag as AnalogInputTag).Alarms.ToList();
                analogInputTag.Alarms = previousAlarms;
                foreach (var alarm in previousAlarms)
                {
                    alarm.AnalogInputTag = analogInputTag;
                }
            }

            _tags[index] = tag;
            RegisterTagWithPlc(tag);
            _repository.SaveTag(tag);
            _logger.Log(string.Format("Ažuriran tag '{0}'.", tag.TagName));
        }

        public bool RemoveTag(string tagName)
        {
            var tag = _tags.FirstOrDefault(t => string.Equals(t.TagName, tagName, StringComparison.OrdinalIgnoreCase));
            if (tag == null)
            {
                return false;
            }

            var analogInputTag = tag as AnalogInputTag;
            if (analogInputTag != null)
            {
                var dependentAlarms = _alarms.Where(a => a.AnalogInputTagId == analogInputTag.Id).ToList();
                foreach (var alarm in dependentAlarms)
                {
                    _alarms.Remove(alarm);
                }
            }

            _tags.Remove(tag);
            _lastScanByTagId.Remove(tag.Id);
            _repository.DeleteTag(tag.Id);
            _logger.Log(string.Format("Uklonjen tag '{0}'.", tag.TagName));
            return true;
        }

        public void AddAlarm(Alarm alarm)
        {
            var analogInputTag = _tags.OfType<AnalogInputTag>().FirstOrDefault(t => t.Id == alarm.AnalogInputTagId);
            var validationErrors = _validationService.ValidateAlarm(alarm, analogInputTag, _alarms).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            alarm.Id = ++_nextAlarmId;
            alarm.AnalogInputTag = analogInputTag;
            analogInputTag.Alarms.Add(alarm);
            _alarms.Add(alarm);
            _repository.SaveAlarm(alarm);
            _logger.Log(string.Format("Dodat alarm '{0}' za AI tag '{1}'.", alarm.Message, analogInputTag.TagName));
        }

        public void UpdateAlarm(Alarm alarm)
        {
            var existingAlarm = _alarms.FirstOrDefault(a => a.Id == alarm.Id);
            if (existingAlarm == null)
            {
                throw new InvalidOperationException("Alarm za izmenu nije pronađen.");
            }

            var analogInputTag = _tags.OfType<AnalogInputTag>().FirstOrDefault(t => t.Id == alarm.AnalogInputTagId);
            var validationErrors = _validationService.ValidateAlarm(alarm, analogInputTag, _alarms).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            if (existingAlarm.AnalogInputTag != null && existingAlarm.AnalogInputTag.Id != alarm.AnalogInputTagId)
            {
                existingAlarm.AnalogInputTag.Alarms.Remove(existingAlarm);
            }

            existingAlarm.Threshold = alarm.Threshold;
            existingAlarm.TriggerType = alarm.TriggerType;
            existingAlarm.Message = alarm.Message;
            existingAlarm.AnalogInputTagId = alarm.AnalogInputTagId;
            existingAlarm.AnalogInputTag = analogInputTag;
            if (analogInputTag != null && !analogInputTag.Alarms.Contains(existingAlarm))
            {
                analogInputTag.Alarms.Add(existingAlarm);
            }

            _repository.SaveAlarm(existingAlarm);
            _logger.Log(string.Format("Ažuriran alarm #{0}.", existingAlarm.Id));
        }

        public bool RemoveAlarm(int alarmId)
        {
            var alarm = _alarms.FirstOrDefault(a => a.Id == alarmId);
            if (alarm == null)
            {
                return false;
            }

            if (alarm.AnalogInputTag != null)
            {
                alarm.AnalogInputTag.Alarms.Remove(alarm);
            }

            _alarms.Remove(alarm);
            _repository.DeleteAlarm(alarmId);
            _logger.Log(string.Format("Uklonjen alarm #{0}.", alarm.Id));
            return true;
        }

        public void WriteOutputValue(string tagName, double value)
        {
            var tag = _tags.OfType<OutputTag>().FirstOrDefault(t => string.Equals(t.TagName, tagName, StringComparison.OrdinalIgnoreCase));
            if (tag == null)
            {
                throw new InvalidOperationException("Izlazni tag nije pronađen.");
            }

            var analogOutputTag = tag as AnalogOutputTag;
            if (analogOutputTag != null && (value < analogOutputTag.LowLimit || value > analogOutputTag.HighLimit))
            {
                throw new InvalidOperationException("AO vrednost mora biti unutar zadatih granica.");
            }

            var digitalOutputTag = tag as DigitalOutputTag;
            if (digitalOutputTag != null)
            {
                value = value >= 0.5 ? 1d : 0d;
            }

            _plcSimulator.Write(tag.IOAddress, value);
            tag.CurrentValue = value;
            tag.LastUpdated = DateTime.Now;
            _repository.SaveTag(tag);
            _logger.Log(string.Format("Upisana vrednost {0:F2} u tag '{1}'.", value, tag.TagName));
            RaiseTagValueChanged(tag);
        }

        public void ToggleScan(string tagName, bool enabled)
        {
            var tag = _tags.OfType<InputTag>().FirstOrDefault(t => string.Equals(t.TagName, tagName, StringComparison.OrdinalIgnoreCase));
            if (tag == null)
            {
                throw new InvalidOperationException("Ulazni tag nije pronađen.");
            }

            tag.OnOffScan = enabled;
            _repository.SaveTag(tag);
            _logger.Log(string.Format("Scan za tag '{0}' je {1}.", tag.TagName, enabled ? "uključen" : "isključen"));
        }

        public void ScanInputs()
        {
            foreach (var inputTag in _tags.OfType<InputTag>().Where(t => t.OnOffScan))
            {
                ScanSingleInput(inputTag);
            }

            _logger.Log("Odradjen scan ulaznih tagova.");
        }

        public void ScanInputsIfDue()
        {
            var now = DateTime.UtcNow;
            foreach (var inputTag in _tags.OfType<InputTag>().Where(t => t.OnOffScan))
            {
                DateTime lastScan;
                if (!_lastScanByTagId.TryGetValue(inputTag.Id, out lastScan))
                {
                    lastScan = DateTime.MinValue;
                }

                if ((now - lastScan).TotalMilliseconds >= inputTag.ScanTime)
                {
                    ScanSingleInput(inputTag);
                    _lastScanByTagId[inputTag.Id] = now;
                }
            }
        }

        public void AcknowledgeAlarm(int alarmId)
        {
            var alarm = _alarms.FirstOrDefault(a => a.Id == alarmId);
            if (alarm == null)
            {
                throw new InvalidOperationException("Alarm nije pronađen.");
            }

            if (alarm.State == AlarmState.Active)
            {
                alarm.State = AlarmState.Acknowledged;
                _repository.SaveAlarm(alarm);

                foreach (var activatedAlarm in _activatedAlarms.Where(a => a.AlarmId == alarmId && a.State == AlarmState.Active))
                {
                    activatedAlarm.State = AlarmState.Acknowledged;
                    _repository.UpdateActivatedAlarm(activatedAlarm);
                }

                RefreshAnalogAlarmFlags(alarm.AnalogInputTag);
                _logger.Log(string.Format("Alarm #{0} je acknowledge-ovan.", alarmId));
            }
        }

        public string GenerateReport(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var filePath = Path.Combine(outputDirectory, string.Format("report_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now));
            var builder = new StringBuilder();
            builder.AppendLine("SCADA report - analogni ulazi u zoni limita +/- 5");
            builder.AppendLine(string.Format("Generisano: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
            builder.AppendLine();

            var analogTags = _tags.OfType<AnalogInputTag>()
                .Where(t => Math.Abs(t.CurrentValue - t.LowLimit) <= 5 || Math.Abs(t.CurrentValue - t.HighLimit) <= 5)
                .OrderBy(t => t.TagName)
                .ToList();

            if (!analogTags.Any())
            {
                builder.AppendLine("Nema analognih ulaza u zoni limita.");
            }
            else
            {
                foreach (var tag in analogTags)
                {
                    builder.AppendLine(string.Format("{0} | Value={1:F2} {2} | Low={3:F2} | High={4:F2} | LastUpdated={5:dd.MM.yyyy HH:mm:ss}",
                        tag.TagName,
                        tag.CurrentValue,
                        tag.Units,
                        tag.LowLimit,
                        tag.HighLimit,
                        tag.LastUpdated));
                }
            }

            File.WriteAllText(filePath, builder.ToString());
            _logger.Log(string.Format("Generisan report '{0}'.", filePath));
            return filePath;
        }

        public void ExportConfiguration(string filePath)
        {
            var export = new ScadaConfigurationExport
            {
                Tags = _tags.Select(CreateExportedTag).ToList(),
                Alarms = _alarms.Select(CreateExportedAlarm).ToList()
            };

            using (var stream = File.Create(filePath))
            {
                var serializer = new DataContractJsonSerializer(typeof(ScadaConfigurationExport));
                serializer.WriteObject(stream, export);
            }

            _logger.Log(string.Format("Export konfiguracije u '{0}'.", filePath));
        }

        public void ImportConfiguration(string filePath, bool replaceExisting)
        {
            ScadaConfigurationExport export;
            using (var stream = File.OpenRead(filePath))
            {
                var serializer = new DataContractJsonSerializer(typeof(ScadaConfigurationExport));
                export = (ScadaConfigurationExport)serializer.ReadObject(stream);
            }

            if (export == null || export.Tags == null)
            {
                throw new InvalidOperationException("Import fajl nije validan.");
            }

            if (replaceExisting)
            {
                _repository.ClearAll();
                _tags.Clear();
                _alarms.Clear();
                _activatedAlarms.Clear();
                _lastScanByTagId.Clear();
                _nextTagId = 0;
                _nextAlarmId = 0;
                _nextActivatedAlarmId = 0;
            }

            var tagNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var exportedTag in export.Tags)
            {
                var tag = CreateTagFromExport(exportedTag);
                AddTag(tag);
                tagNameToId[tag.TagName] = tag.Id;
            }

            if (export.Alarms != null)
            {
                foreach (var exportedAlarm in export.Alarms)
                {
                    int analogInputTagId;
                    if (!tagNameToId.TryGetValue(exportedAlarm.AnalogInputTagName, out analogInputTagId))
                    {
                        continue;
                    }

                    AddAlarm(new Alarm(exportedAlarm.Threshold, exportedAlarm.TriggerType, exportedAlarm.Message, analogInputTagId));
                }
            }

            _logger.Log(string.Format("Import konfiguracije iz '{0}'.", filePath));
        }

        private void ScanSingleInput(InputTag inputTag)
        {
            var rawValue = _plcSimulator.Read(inputTag.IOAddress);
            var shouldUpdate = true;
            var newValue = rawValue;
            var analogInputTag = inputTag as AnalogInputTag;
            var digitalInputTag = inputTag as DigitalInputTag;

            if (analogInputTag != null)
            {
                newValue = Math.Max(analogInputTag.LowLimit, Math.Min(analogInputTag.HighLimit, rawValue));
                if (inputTag.LastUpdated != DateTime.MinValue && Math.Abs(newValue - inputTag.CurrentValue) < analogInputTag.Deadband)
                {
                    shouldUpdate = false;
                }
            }
            else if (digitalInputTag != null)
            {
                newValue = rawValue >= 0.5 ? 1d : 0d;
            }

            if (!shouldUpdate)
            {
                return;
            }

            inputTag.CurrentValue = newValue;
            inputTag.LastUpdated = DateTime.Now;
            _repository.SaveTag(inputTag);
            RaiseTagValueChanged(inputTag);

            if (analogInputTag != null)
            {
                EvaluateAlarms(analogInputTag);
            }
        }

        private void EvaluateAlarms(AnalogInputTag analogInputTag)
        {
            foreach (var alarm in analogInputTag.Alarms)
            {
                var shouldActivate = ShouldActivate(alarm, analogInputTag.CurrentValue, analogInputTag.Hysteresis);
                var shouldReset = ShouldReset(alarm, analogInputTag.CurrentValue, analogInputTag.Hysteresis);

                if (alarm.State == AlarmState.Inactive && shouldActivate)
                {
                    alarm.State = AlarmState.Active;
                    _repository.SaveAlarm(alarm);

                    var activatedAlarm = new ActivatedAlarm(alarm.Id, analogInputTag.TagName, alarm.Message, analogInputTag.CurrentValue)
                    {
                        Id = ++_nextActivatedAlarmId,
                        State = AlarmState.Active
                    };
                    _activatedAlarms.Insert(0, activatedAlarm);
                    _repository.SaveActivatedAlarm(activatedAlarm);
                    _logger.Log(string.Format("Aktiviran alarm #{0} nad tagom '{1}'.", alarm.Id, analogInputTag.TagName));
                    RaiseAlarmRaised(alarm, activatedAlarm);
                }
                else if ((alarm.State == AlarmState.Active || alarm.State == AlarmState.Acknowledged) && shouldReset)
                {
                    alarm.State = AlarmState.Inactive;
                    _repository.SaveAlarm(alarm);

                    foreach (var activatedAlarm in _activatedAlarms.Where(a => a.AlarmId == alarm.Id && a.State != AlarmState.Inactive))
                    {
                        activatedAlarm.State = AlarmState.Inactive;
                        _repository.UpdateActivatedAlarm(activatedAlarm);
                    }

                    _logger.Log(string.Format("Alarm #{0} je deaktiviran.", alarm.Id));
                }
            }

            RefreshAnalogAlarmFlags(analogInputTag);
        }

        private static bool ShouldActivate(Alarm alarm, double value, double hysteresis)
        {
            if (alarm.TriggerType == AlarmTriggerType.AboveLimit)
            {
                return value >= alarm.Threshold + hysteresis;
            }

            return value <= alarm.Threshold - hysteresis;
        }

        private static bool ShouldReset(Alarm alarm, double value, double hysteresis)
        {
            if (alarm.TriggerType == AlarmTriggerType.AboveLimit)
            {
                return value < alarm.Threshold - hysteresis;
            }

            return value > alarm.Threshold + hysteresis;
        }

        private void RefreshAnalogAlarmFlags(AnalogInputTag analogInputTag)
        {
            if (analogInputTag == null)
            {
                return;
            }

            analogInputTag.IsInAlarm = analogInputTag.Alarms.Any(a => a.State == AlarmState.Active || a.State == AlarmState.Acknowledged);
            analogInputTag.HasUnacknowledgedAlarm = analogInputTag.Alarms.Any(a => a.State == AlarmState.Active);
        }

        private void RegisterTagWithPlc(TagBase tag)
        {
            var outputTag = tag as OutputTag;
            if (outputTag != null)
            {
                _plcSimulator.EnsureAddress(tag.IOAddress, outputTag.InitialValue);
                _plcSimulator.Write(tag.IOAddress, tag.CurrentValue != 0 || tag.LastUpdated != DateTime.MinValue ? tag.CurrentValue : outputTag.InitialValue);
                if (tag.LastUpdated == DateTime.MinValue)
                {
                    tag.CurrentValue = outputTag.InitialValue;
                }
            }
            else
            {
                _plcSimulator.EnsureAddress(tag.IOAddress, tag.CurrentValue);
            }
        }

        private static ExportedTag CreateExportedTag(TagBase tag)
        {
            var exported = new ExportedTag
            {
                TagType = tag.TagType,
                TagName = tag.TagName,
                Description = tag.Description,
                IOAddress = tag.IOAddress
            };

            var inputTag = tag as InputTag;
            if (inputTag != null)
            {
                exported.ScanTime = inputTag.ScanTime;
                exported.OnOffScan = inputTag.OnOffScan;
            }

            var analogInputTag = tag as AnalogInputTag;
            if (analogInputTag != null)
            {
                exported.LowLimit = analogInputTag.LowLimit;
                exported.HighLimit = analogInputTag.HighLimit;
                exported.Units = analogInputTag.Units;
                exported.Deadband = analogInputTag.Deadband;
                exported.Hysteresis = analogInputTag.Hysteresis;
            }

            var analogOutputTag = tag as AnalogOutputTag;
            if (analogOutputTag != null)
            {
                exported.LowLimit = analogOutputTag.LowLimit;
                exported.HighLimit = analogOutputTag.HighLimit;
                exported.Units = analogOutputTag.Units;
                exported.InitialValue = analogOutputTag.InitialValue;
            }

            var digitalOutputTag = tag as DigitalOutputTag;
            if (digitalOutputTag != null)
            {
                exported.InitialValue = digitalOutputTag.InitialValue;
            }

            return exported;
        }

        private static ExportedAlarm CreateExportedAlarm(Alarm alarm)
        {
            return new ExportedAlarm
            {
                AnalogInputTagName = alarm.AnalogInputTag != null ? alarm.AnalogInputTag.TagName : string.Empty,
                Threshold = alarm.Threshold,
                TriggerType = alarm.TriggerType,
                Message = alarm.Message
            };
        }

        private static TagBase CreateTagFromExport(ExportedTag exportedTag)
        {
            switch (exportedTag.TagType)
            {
                case TagType.AI:
                    return new AnalogInputTag(
                        exportedTag.TagName,
                        exportedTag.Description,
                        exportedTag.IOAddress,
                        exportedTag.ScanTime ?? 1000,
                        exportedTag.OnOffScan ?? true,
                        exportedTag.LowLimit ?? 0,
                        exportedTag.HighLimit ?? 100,
                        exportedTag.Units ?? string.Empty,
                        exportedTag.Deadband ?? 0,
                        exportedTag.Hysteresis ?? 0);
                case TagType.AO:
                    return new AnalogOutputTag(
                        exportedTag.TagName,
                        exportedTag.Description,
                        exportedTag.IOAddress,
                        exportedTag.InitialValue ?? 0,
                        exportedTag.LowLimit ?? 0,
                        exportedTag.HighLimit ?? 100,
                        exportedTag.Units ?? string.Empty);
                case TagType.DI:
                    return new DigitalInputTag(
                        exportedTag.TagName,
                        exportedTag.Description,
                        exportedTag.IOAddress,
                        exportedTag.ScanTime ?? 1000,
                        exportedTag.OnOffScan ?? true);
                case TagType.DO:
                    return new DigitalOutputTag(
                        exportedTag.TagName,
                        exportedTag.Description,
                        exportedTag.IOAddress,
                        exportedTag.InitialValue ?? 0);
                default:
                    throw new InvalidOperationException("Nepoznat tip taga u import fajlu.");
            }
        }

        private void RaiseTagValueChanged(TagBase tag)
        {
            var handler = TagValueChanged;
            if (handler != null)
            {
                handler(this, new TagValueChangedEventArgs(tag));
            }
        }

        private void RaiseAlarmRaised(Alarm alarm, ActivatedAlarm activatedAlarm)
        {
            var handler = AlarmRaised;
            if (handler != null)
            {
                handler(this, new AlarmRaisedEventArgs(alarm, activatedAlarm));
            }
        }
    }
}
