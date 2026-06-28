using System.Collections.Generic;
using System.Runtime.Serialization;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    [DataContract]
    public class ScadaConfigurationExport
    {
        [DataMember]
        public IList<ExportedTag> Tags { get; set; }

        [DataMember]
        public IList<ExportedAlarm> Alarms { get; set; }
    }

    [DataContract]
    public class ExportedTag
    {
        [DataMember]
        public TagType TagType { get; set; }

        [DataMember]
        public string TagName { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string IOAddress { get; set; }

        [DataMember]
        public int? ScanTime { get; set; }

        [DataMember]
        public bool? OnOffScan { get; set; }

        [DataMember]
        public double? LowLimit { get; set; }

        [DataMember]
        public double? HighLimit { get; set; }

        [DataMember]
        public string Units { get; set; }

        [DataMember]
        public double? Deadband { get; set; }

        [DataMember]
        public double? Hysteresis { get; set; }

        [DataMember]
        public double? InitialValue { get; set; }
    }

    [DataContract]
    public class ExportedAlarm
    {
        [DataMember]
        public string AnalogInputTagName { get; set; }

        [DataMember]
        public double Threshold { get; set; }

        [DataMember]
        public AlarmTriggerType TriggerType { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}
