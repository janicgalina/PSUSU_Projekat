using System;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Data.Entities
{
    public class ActivatedAlarmEntity
    {
        public int Id { get; set; }
        public int AlarmId { get; set; }
        public string TagName { get; set; }
        public string Message { get; set; }
        public DateTime ActivationTime { get; set; }
        public double Value { get; set; }
        public AlarmState State { get; set; }
    }
}
