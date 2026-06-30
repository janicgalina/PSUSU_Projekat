using System;

namespace ProjekatScada.Models
{
    public class LimitZoneHistoryRecord
    {
        public string TagName { get; set; }
        public double Value { get; set; }
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
