using System;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Data.Entities
{
    public class TagEntity
    {
        public int Id { get; set; }
        public TagType TagType { get; set; }
        public string TagName { get; set; }
        public string Description { get; set; }
        public string IOAddress { get; set; }
        public double CurrentValue { get; set; }
        public DateTime LastUpdated { get; set; }
        public int? ScanTime { get; set; }
        public bool? OnOffScan { get; set; }
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string Units { get; set; }
        public double? Deadband { get; set; }
        public double? Hysteresis { get; set; }
        public double? InitialValue { get; set; }
    }
}
