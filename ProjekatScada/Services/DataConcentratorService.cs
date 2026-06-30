using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            foreach (var analogTag in _tags.OfType<AnalogInputTag>())
            {
                EvaluateAlarms(analogTag);
            }
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

            if (existingTag.GetType() != tag.GetType())
            {
                throw new InvalidOperationException("Tip taga se ne može menjati pri izmeni.");
            }

            ApplyTagChanges(existingTag, tag);
            RegisterTagWithPlc(existingTag);
            _repository.SaveTag(existingTag);
            _logger.Log(string.Format("Ažuriran tag '{0}'.", existingTag.TagName));
        }

        private static void ApplyTagChanges(TagBase target, TagBase source)
        {
            target.TagName = source.TagName;
            target.Description = source.Description;
            target.IOAddress = source.IOAddress;

            var targetInput = target as InputTag;
            var sourceInput = source as InputTag;
            if (targetInput != null && sourceInput != null)
            {
                targetInput.ScanTime = sourceInput.ScanTime;
                targetInput.OnOffScan = sourceInput.OnOffScan;
            }

            var targetAnalogInput = target as AnalogInputTag;
            var sourceAnalogInput = source as AnalogInputTag;
            if (targetAnalogInput != null && sourceAnalogInput != null)
            {
                targetAnalogInput.LowLimit = sourceAnalogInput.LowLimit;
                targetAnalogInput.HighLimit = sourceAnalogInput.HighLimit;
                targetAnalogInput.Units = sourceAnalogInput.Units;
                targetAnalogInput.Deadband = sourceAnalogInput.Deadband;
                targetAnalogInput.Hysteresis = sourceAnalogInput.Hysteresis;
            }

            var targetAnalogOutput = target as AnalogOutputTag;
            var sourceAnalogOutput = source as AnalogOutputTag;
            if (targetAnalogOutput != null && sourceAnalogOutput != null)
            {
                targetAnalogOutput.LowLimit = sourceAnalogOutput.LowLimit;
                targetAnalogOutput.HighLimit = sourceAnalogOutput.HighLimit;
                targetAnalogOutput.Units = sourceAnalogOutput.Units;
                targetAnalogOutput.InitialValue = sourceAnalogOutput.InitialValue;
            }

            var targetDigitalOutput = target as DigitalOutputTag;
            var sourceDigitalOutput = source as DigitalOutputTag;
            if (targetDigitalOutput != null && sourceDigitalOutput != null)
            {
                targetDigitalOutput.InitialValue = sourceDigitalOutput.InitialValue;
            }
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
            builder.AppendLine("SCADA report - istorija analognih ulaza u zoni limita +/- 5");
            builder.AppendLine(string.Format("Generisano: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
            builder.AppendLine();

            var records = _repository.GetAnalogHistoryNearLimits(5);

            if (!records.Any())
            {
                builder.AppendLine("Nema zapisa u istoriji gde je vrednost bila u zoni low/high limit +/- 5.");
            }
            else
            {
                builder.AppendLine("Tag | Vreme | Vrednost | Low | High | Units");
                builder.AppendLine(new string('-', 90));
                foreach (var record in records)
                {
                    builder.AppendLine(string.Format("{0} | {1:dd.MM.yyyy HH:mm:ss} | {2:F2} | {3:F2} | {4:F2} | {5}",
                        record.TagName,
                        record.RecordedAt,
                        record.Value,
                        record.LowLimit,
                        record.HighLimit,
                        record.Units));
                }
            }

            File.WriteAllText(filePath, builder.ToString());
            _logger.Log(string.Format("Generisan report '{0}' ({1} istorijskih zapisa).", filePath, records.Count));
            return filePath;
        }

        public ActivatedAlarm GetActivatedAlarmFromDatabase(int activatedAlarmId)
        {
            return _repository.GetActivatedAlarmById(activatedAlarmId);
        }

        public void ReloadActivatedAlarmsFromDatabase()
        {
            var fromDatabase = _repository.GetActivatedAlarmsFromDatabase();
            _activatedAlarms.Clear();
            _activatedAlarms.AddRange(fromDatabase);

            if (fromDatabase.Any())
            {
                _nextActivatedAlarmId = fromDatabase.Max(a => a.Id);
            }
        }

        public string GenerateTagValueHistoryReport(TagValueHistoryFilter filter, string outputDirectory)
        {
            var records = _repository.SearchTagValueHistory(filter);
            Directory.CreateDirectory(outputDirectory);
            var filePath = Path.Combine(outputDirectory, string.Format("ai_search_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now));
            var builder = new StringBuilder();
            builder.AppendLine("SCADA report - pretraga vrednosti AI tagova");
            builder.AppendLine(string.Format("Generisano: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
            builder.AppendLine();
            builder.AppendLine("Uslovi pretrage:");
            builder.AppendLine(string.Format("  Tag: {0}", string.IsNullOrWhiteSpace(filter.TagName) ? "svi" : filter.TagName));
            builder.AppendLine(string.Format("  Vreme od: {0}", filter.FromTime.HasValue ? filter.FromTime.Value.ToString("dd.MM.yyyy HH:mm:ss") : "nije zadato"));
            builder.AppendLine(string.Format("  Vreme do: {0}", filter.ToTime.HasValue ? filter.ToTime.Value.ToString("dd.MM.yyyy HH:mm:ss") : "nije zadato"));
            builder.AppendLine(string.Format("  Vrednost od: {0}", filter.FromValue.HasValue ? filter.FromValue.Value.ToString("F2") : "nije zadato"));
            builder.AppendLine(string.Format("  Vrednost do: {0}", filter.ToValue.HasValue ? filter.ToValue.Value.ToString("F2") : "nije zadato"));
            builder.AppendLine();

            if (!records.Any())
            {
                builder.AppendLine("Nema zapisa koji zadovoljavaju zadate uslove.");
            }
            else
            {
                builder.AppendLine("Tag | Vreme | Vrednost");
                builder.AppendLine(new string('-', 60));
                foreach (var record in records)
                {
                    builder.AppendLine(string.Format("{0} | {1:dd.MM.yyyy HH:mm:ss} | {2:F2}",
                        record.TagName,
                        record.RecordedAt,
                        record.Value));
                }
            }

            File.WriteAllText(filePath, builder.ToString());
            _logger.Log(string.Format("Generisan AI search report '{0}' ({1} zapisa).", filePath, records.Count));
            return filePath;
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
                _repository.SaveTagValueHistory(analogInputTag);
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

                    var activatedAlarmFromDatabase = _repository.GetActivatedAlarmById(activatedAlarm.Id) ?? activatedAlarm;
                    RaiseAlarmRaised(alarm, activatedAlarmFromDatabase);
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
