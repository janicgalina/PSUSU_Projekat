using System;
using System.Collections.Generic;
using ProjekatScada.Models;

namespace ProjekatScada.Services.Interfaces
{
    public interface IDataConcentratorService
    {
        event EventHandler<TagValueChangedEventArgs> TagValueChanged;
        event EventHandler<AlarmRaisedEventArgs> AlarmRaised;

        IReadOnlyCollection<TagBase> Tags { get; }
        IReadOnlyCollection<Alarm> Alarms { get; }
        IReadOnlyCollection<ActivatedAlarm> ActivatedAlarms { get; }

        void LoadFromRepository();
        void AddTag(TagBase tag);
        void UpdateTag(TagBase tag);
        bool RemoveTag(string tagName);
        void AddAlarm(Alarm alarm);
        void UpdateAlarm(Alarm alarm);
        bool RemoveAlarm(int alarmId);
        void WriteOutputValue(string tagName, double value);
        void ToggleScan(string tagName, bool enabled);
        void ScanInputs();
        void ScanInputsIfDue();
        void AcknowledgeAlarm(int alarmId);
        string GenerateReport(string outputDirectory);
        void ExportConfiguration(string filePath);
        void ImportConfiguration(string filePath, bool replaceExisting);
        string GenerateTagValueHistoryReport(TagValueHistoryFilter filter, string outputDirectory);
    }

    public class TagValueChangedEventArgs : EventArgs
    {
        public TagValueChangedEventArgs(TagBase tag)
        {
            Tag = tag;
        }

        public TagBase Tag { get; private set; }
    }

    public class AlarmRaisedEventArgs : EventArgs
    {
        public AlarmRaisedEventArgs(Alarm alarm, ActivatedAlarm activatedAlarm)
        {
            Alarm = alarm;
            ActivatedAlarm = activatedAlarm;
        }

        public Alarm Alarm { get; private set; }
        public ActivatedAlarm ActivatedAlarm { get; private set; }
    }
}
