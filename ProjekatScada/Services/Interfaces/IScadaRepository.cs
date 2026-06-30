using System.Collections.Generic;
using ProjekatScada.Models;

namespace ProjekatScada.Services.Interfaces
{
    public interface IScadaRepository
    {
        ScadaPersistedData LoadAll();
        void SaveTag(TagBase tag);
        void DeleteTag(int tagId);
        void SaveAlarm(Alarm alarm);
        void DeleteAlarm(int alarmId);
        void SaveActivatedAlarm(ActivatedAlarm activatedAlarm);
        void UpdateActivatedAlarm(ActivatedAlarm activatedAlarm);
        void SaveTagValueHistory(AnalogInputTag tag);
        IList<TagValueHistoryRecord> SearchTagValueHistory(TagValueHistoryFilter filter);
        ActivatedAlarm GetActivatedAlarmById(int activatedAlarmId);
        IList<ActivatedAlarm> GetActivatedAlarmsFromDatabase();
        IList<LimitZoneHistoryRecord> GetAnalogHistoryNearLimits(double margin);
        void ReplaceAll(ScadaPersistedData data);
        void ClearAll();
    }

    public class ScadaPersistedData
    {
        public IList<TagBase> Tags { get; set; }
        public IList<Alarm> Alarms { get; set; }
        public IList<ActivatedAlarm> ActivatedAlarms { get; set; }
        public int NextTagId { get; set; }
        public int NextAlarmId { get; set; }
        public int NextActivatedAlarmId { get; set; }
    }
}
