using System;
using System.Collections.Generic;
using System.Linq;
using ProjekatScada.Data;
using ProjekatScada.Data.Entities;
using ProjekatScada.Models;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.Data
{
    public class ScadaRepository : IScadaRepository
    {
        public ScadaPersistedData LoadAll()
        {
            using (var context = new ScadaDbContext())
            {
                context.Database.Initialize(false);

                var tagEntities = context.Tags.AsNoTracking().ToList();
                var tags = tagEntities.Select(ScadaMapper.ToDomain).ToList();
                var analogTagsById = tags.OfType<AnalogInputTag>().ToDictionary(t => t.Id);

                var alarmEntities = context.Alarms.AsNoTracking().ToList();
                var alarms = alarmEntities
                    .Select(entity => ScadaMapper.ToDomain(entity, analogTagsById))
                    .ToList();

                var activatedAlarms = context.ActivatedAlarms
                    .AsNoTracking()
                    .OrderByDescending(a => a.ActivationTime)
                    .Select(ScadaMapper.ToDomain)
                    .ToList();

                foreach (var analogTag in analogTagsById.Values)
                {
                    RefreshAnalogAlarmFlags(analogTag);
                }

                return new ScadaPersistedData
                {
                    Tags = tags,
                    Alarms = alarms,
                    ActivatedAlarms = activatedAlarms,
                    NextTagId = tagEntities.Any() ? tagEntities.Max(t => t.Id) : 0,
                    NextAlarmId = alarmEntities.Any() ? alarmEntities.Max(a => a.Id) : 0,
                    NextActivatedAlarmId = activatedAlarms.Any() ? activatedAlarms.Max(a => a.Id) : 0
                };
            }
        }

        public void SaveTag(TagBase tag)
        {
            using (var context = new ScadaDbContext())
            {
                var entity = ScadaMapper.ToEntity(tag);
                var existing = context.Tags.Find(tag.Id);
                if (existing == null)
                {
                    context.Tags.Add(entity);
                }
                else
                {
                    context.Entry(existing).CurrentValues.SetValues(entity);
                }

                context.SaveChanges();
            }
        }

        public void DeleteTag(int tagId)
        {
            using (var context = new ScadaDbContext())
            {
                var entity = context.Tags.Find(tagId);
                if (entity != null)
                {
                    var historyRecords = context.TagValueHistory.Where(record => record.TagId == tagId).ToList();
                    context.TagValueHistory.RemoveRange(historyRecords);

                    var dependentAlarms = context.Alarms.Where(a => a.AnalogInputTagId == tagId).ToList();
                    context.Alarms.RemoveRange(dependentAlarms);
                    context.Tags.Remove(entity);
                    context.SaveChanges();
                }
            }
        }

        public void SaveAlarm(Alarm alarm)
        {
            using (var context = new ScadaDbContext())
            {
                var entity = ScadaMapper.ToEntity(alarm);
                var existing = context.Alarms.Find(alarm.Id);
                if (existing == null)
                {
                    context.Alarms.Add(entity);
                }
                else
                {
                    context.Entry(existing).CurrentValues.SetValues(entity);
                }

                context.SaveChanges();
            }
        }

        public void DeleteAlarm(int alarmId)
        {
            using (var context = new ScadaDbContext())
            {
                var entity = context.Alarms.Find(alarmId);
                if (entity != null)
                {
                    context.Alarms.Remove(entity);
                    context.SaveChanges();
                }
            }
        }

        public void SaveActivatedAlarm(ActivatedAlarm activatedAlarm)
        {
            using (var context = new ScadaDbContext())
            {
                var entity = ScadaMapper.ToEntity(activatedAlarm);
                context.ActivatedAlarms.Add(entity);
                context.SaveChanges();
            }
        }

        public void UpdateActivatedAlarm(ActivatedAlarm activatedAlarm)
        {
            using (var context = new ScadaDbContext())
            {
                var existing = context.ActivatedAlarms.Find(activatedAlarm.Id);
                if (existing != null)
                {
                    existing.State = activatedAlarm.State;
                    context.SaveChanges();
                }
            }
        }

        public void SaveTagValueHistory(AnalogInputTag tag)
        {
            using (var context = new ScadaDbContext())
            {
                context.TagValueHistory.Add(new TagValueHistoryEntity
                {
                    TagId = tag.Id,
                    TagName = tag.TagName,
                    Value = tag.CurrentValue,
                    RecordedAt = tag.LastUpdated
                });
                context.SaveChanges();
            }
        }

        public IList<TagValueHistoryRecord> SearchTagValueHistory(TagValueHistoryFilter filter)
        {
            using (var context = new ScadaDbContext())
            {
                var query = context.TagValueHistory.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.TagName))
                {
                    query = query.Where(record => record.TagName == filter.TagName);
                }

                if (filter.FromTime.HasValue)
                {
                    query = query.Where(record => record.RecordedAt >= filter.FromTime.Value);
                }

                if (filter.ToTime.HasValue)
                {
                    query = query.Where(record => record.RecordedAt <= filter.ToTime.Value);
                }

                if (filter.FromValue.HasValue)
                {
                    query = query.Where(record => record.Value >= filter.FromValue.Value);
                }

                if (filter.ToValue.HasValue)
                {
                    query = query.Where(record => record.Value <= filter.ToValue.Value);
                }

                return query
                    .OrderBy(record => record.RecordedAt)
                    .ThenBy(record => record.TagName)
                    .Select(record => new TagValueHistoryRecord
                    {
                        Id = record.Id,
                        TagId = record.TagId,
                        TagName = record.TagName,
                        Value = record.Value,
                        RecordedAt = record.RecordedAt
                    })
                    .ToList();
            }
        }

        public void ClearAll()
        {
            using (var context = new ScadaDbContext())
            {
                context.TagValueHistory.RemoveRange(context.TagValueHistory);
                context.ActivatedAlarms.RemoveRange(context.ActivatedAlarms);
                context.Alarms.RemoveRange(context.Alarms);
                context.Tags.RemoveRange(context.Tags);
                context.SaveChanges();
            }
        }

        public void ReplaceAll(ScadaPersistedData data)
        {
            using (var context = new ScadaDbContext())
            {
                context.TagValueHistory.RemoveRange(context.TagValueHistory);
                context.ActivatedAlarms.RemoveRange(context.ActivatedAlarms);
                context.Alarms.RemoveRange(context.Alarms);
                context.Tags.RemoveRange(context.Tags);
                context.SaveChanges();

                foreach (var tag in data.Tags)
                {
                    context.Tags.Add(ScadaMapper.ToEntity(tag));
                }

                foreach (var alarm in data.Alarms)
                {
                    context.Alarms.Add(ScadaMapper.ToEntity(alarm));
                }

                foreach (var activatedAlarm in data.ActivatedAlarms)
                {
                    context.ActivatedAlarms.Add(ScadaMapper.ToEntity(activatedAlarm));
                }

                context.SaveChanges();
            }
        }

        private static void RefreshAnalogAlarmFlags(AnalogInputTag analogInputTag)
        {
            analogInputTag.IsInAlarm = analogInputTag.Alarms.Any(a =>
                a.State == Models.Enums.AlarmState.Active || a.State == Models.Enums.AlarmState.Acknowledged);
            analogInputTag.HasUnacknowledgedAlarm = analogInputTag.Alarms.Any(a =>
                a.State == Models.Enums.AlarmState.Active);
        }
    }
}
