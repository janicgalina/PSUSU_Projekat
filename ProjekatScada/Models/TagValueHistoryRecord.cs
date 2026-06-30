using System;

namespace ProjekatScada.Models
{
    public class TagValueHistoryRecord
    {
        public int Id { get; set; }
        public int TagId { get; set; }
        public string TagName { get; set; }
        public double Value { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
