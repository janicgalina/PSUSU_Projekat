using System;

namespace ProjekatScada.Models
{
    public class TagValueHistoryFilter
    {
        public string TagName { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public double? FromValue { get; set; }
        public double? ToValue { get; set; }
    }
}
