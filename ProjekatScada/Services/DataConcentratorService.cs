using System;
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
        private readonly List<TagBase> _tags = new List<TagBase>();
        private readonly List<Alarm> _alarms = new List<Alarm>();
        private readonly List<ActivatedAlarm> _activatedAlarms = new List<ActivatedAlarm>();
        private int _nextTagId;
        private int _nextAlarmId;
        private int _nextActivatedAlarmId;

        public DataConcentratorService(IPlcSimulator plcSimulator, ITagValidationService validationService, ISystemLogger logger)
        {
            _plcSimulator = plcSimulator;
            _validationService = validationService;
            _logger = logger;
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

        public void AddTag(TagBase tag)
        {
            var validationErrors = _validationService.ValidateTag(tag, _tags).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            tag.Id = ++_nextTagId;
            tag.LastUpdated = DateTime.Now;

            var outputTag = tag as OutputTag;
            if (outputTag != null)
            {
                _plcSimulator.EnsureAddress(tag.IOAddress, outputTag.InitialValue);
                _plcSimulator.Write(tag.IOAddress, outputTag.InitialValue);
                tag.CurrentValue = outputTag.InitialValue;
            }
            else
            {
                _plcSimulator.EnsureAddress(tag.IOAddress);
            }

            _tags.Add(tag);
            _logger.Log(string.Format("Dodat tag '{0}' tipa {1}.", tag.TagName, tag.TagType));
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
            _logger.Log(string.Format("Dodat alarm '{0}' za AI tag '{1}'.", alarm.Message, analogInputTag.TagName));
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
            _logger.Log(string.Format("Scan za tag '{0}' je {1}.", tag.TagName, enabled ? "uključen" : "isključen"));
        }

        public void ScanInputs()
        {
            foreach (var inputTag in _tags.OfType<InputTag>().Where(t => t.OnOffScan))
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
                    continue;
                }

                inputTag.CurrentValue = newValue;
                inputTag.LastUpdated = DateTime.Now;
                RaiseTagValueChanged(inputTag);

                if (analogInputTag != null)
                {
                    EvaluateAlarms(analogInputTag);
                }
            }

            _logger.Log("Odradjen scan ulaznih tagova.");
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
                foreach (var activatedAlarm in _activatedAlarms.Where(a => a.AlarmId == alarmId && a.State == AlarmState.Active))
                {
                    activatedAlarm.State = AlarmState.Acknowledged;
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

        private void EvaluateAlarms(AnalogInputTag analogInputTag)
        {
            foreach (var alarm in analogInputTag.Alarms)
            {
                var shouldActivate = ShouldActivate(alarm, analogInputTag.CurrentValue, analogInputTag.Hysteresis);
                var shouldReset = ShouldReset(alarm, analogInputTag.CurrentValue, analogInputTag.Hysteresis);

                if (alarm.State == AlarmState.Inactive && shouldActivate)
                {
                    alarm.State = AlarmState.Active;
                    var activatedAlarm = new ActivatedAlarm(alarm.Id, analogInputTag.TagName, alarm.Message, analogInputTag.CurrentValue)
                    {
                        Id = ++_nextActivatedAlarmId,
                        State = AlarmState.Active
                    };
                    _activatedAlarms.Insert(0, activatedAlarm);
                    _logger.Log(string.Format("Aktiviran alarm #{0} nad tagom '{1}'.", alarm.Id, analogInputTag.TagName));
                    RaiseAlarmRaised(alarm, activatedAlarm);
                }
                else if ((alarm.State == AlarmState.Active || alarm.State == AlarmState.Acknowledged) && shouldReset)
                {
                    alarm.State = AlarmState.Inactive;
                    foreach (var activatedAlarm in _activatedAlarms.Where(a => a.AlarmId == alarm.Id && a.State != AlarmState.Inactive))
                    {
                        activatedAlarm.State = AlarmState.Inactive;
                    }
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
