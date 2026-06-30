using System;
using System.Collections.Generic;
using System.Linq;
using ProjekatScada.Data.Entities;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Data
{
    public static class ScadaMapper
    {
        public static TagBase ToDomain(TagEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            TagBase tag;
            switch (entity.TagType)
            {
                case TagType.AI:
                    tag = new AnalogInputTag(
                        entity.TagName,
                        entity.Description,
                        entity.IOAddress,
                        entity.ScanTime ?? 1000,
                        entity.OnOffScan ?? true,
                        entity.LowLimit ?? 0,
                        entity.HighLimit ?? 100,
                        entity.Units ?? string.Empty,
                        entity.Deadband ?? 0,
                        entity.Hysteresis ?? 0);
                    break;
                case TagType.AO:
                    tag = new AnalogOutputTag(
                        entity.TagName,
                        entity.Description,
                        entity.IOAddress,
                        entity.InitialValue ?? 0,
                        entity.LowLimit ?? 0,
                        entity.HighLimit ?? 100,
                        entity.Units ?? string.Empty);
                    break;
                case TagType.DI:
                    tag = new DigitalInputTag(
                        entity.TagName,
                        entity.Description,
                        entity.IOAddress,
                        entity.ScanTime ?? 1000,
                        entity.OnOffScan ?? true);
                    break;
                case TagType.DO:
                    tag = new DigitalOutputTag(
                        entity.TagName,
                        entity.Description,
                        entity.IOAddress,
                        entity.InitialValue ?? 0);
                    break;
                default:
                    throw new InvalidOperationException("Nepoznat tip taga.");
            }

            tag.Id = entity.Id;
            tag.CurrentValue = entity.CurrentValue;
            tag.LastUpdated = entity.LastUpdated;
            return tag;
        }

        public static TagEntity ToEntity(TagBase tag)
        {
            var entity = new TagEntity
            {
                Id = tag.Id,
                TagType = tag.TagType,
                TagName = tag.TagName,
                Description = tag.Description,
                IOAddress = tag.IOAddress,
                CurrentValue = tag.CurrentValue,
                LastUpdated = tag.LastUpdated
            };

            var inputTag = tag as InputTag;
            if (inputTag != null)
            {
                entity.ScanTime = inputTag.ScanTime;
                entity.OnOffScan = inputTag.OnOffScan;
            }

            var analogInputTag = tag as AnalogInputTag;
            if (analogInputTag != null)
            {
                entity.LowLimit = analogInputTag.LowLimit;
                entity.HighLimit = analogInputTag.HighLimit;
                entity.Units = analogInputTag.Units;
                entity.Deadband = analogInputTag.Deadband;
                entity.Hysteresis = analogInputTag.Hysteresis;
            }

            var analogOutputTag = tag as AnalogOutputTag;
            if (analogOutputTag != null)
            {
                entity.LowLimit = analogOutputTag.LowLimit;
                entity.HighLimit = analogOutputTag.HighLimit;
                entity.Units = analogOutputTag.Units;
                entity.InitialValue = analogOutputTag.InitialValue;
            }

            var digitalOutputTag = tag as DigitalOutputTag;
            if (digitalOutputTag != null)
            {
                entity.InitialValue = digitalOutputTag.InitialValue;
            }

            return entity;
        }

        public static Alarm ToDomain(AlarmEntity entity, IDictionary<int, AnalogInputTag> analogTagsById)
        {
            var alarm = new Alarm(entity.Threshold, entity.TriggerType, entity.Message, entity.AnalogInputTagId)
            {
                Id = entity.Id,
                State = entity.State
            };

            AnalogInputTag analogInputTag;
            if (analogTagsById != null && analogTagsById.TryGetValue(entity.AnalogInputTagId, out analogInputTag))
            {
                alarm.AnalogInputTag = analogInputTag;
                analogInputTag.Alarms.Add(alarm);
            }

            return alarm;
        }

        public static AlarmEntity ToEntity(Alarm alarm)
        {
            return new AlarmEntity
            {
                Id = alarm.Id,
                Threshold = alarm.Threshold,
                TriggerType = alarm.TriggerType,
                Message = alarm.Message,
                State = alarm.State,
                AnalogInputTagId = alarm.AnalogInputTagId
            };
        }

        public static ActivatedAlarm ToDomain(ActivatedAlarmEntity entity)
        {
            return new ActivatedAlarm(entity.AlarmId, entity.TagName, entity.Message, entity.Value)
            {
                Id = entity.Id,
                ActivationTime = entity.ActivationTime,
                State = entity.State
            };
        }

        public static ActivatedAlarmEntity ToEntity(ActivatedAlarm activatedAlarm)
        {
            return new ActivatedAlarmEntity
            {
                Id = activatedAlarm.Id,
                AlarmId = activatedAlarm.AlarmId,
                TagName = activatedAlarm.TagName,
                Message = activatedAlarm.Message,
                ActivationTime = activatedAlarm.ActivationTime,
                Value = activatedAlarm.Value,
                State = activatedAlarm.State
            };
        }
    }
}
