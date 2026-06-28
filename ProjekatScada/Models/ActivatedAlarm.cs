using System;

namespace ProjekatScada.Models
{
    public class ActivatedAlarm
    {
        public int Id { get; set; }
        public int AlarmId { get; set; }
        public string TagName { get; set; }
        public string Message { get; set; }
        public DateTime ActivationTime { get; set; }
        public double Value { get; set; }

        public ActivatedAlarm()
        {
            ActivationTime = DateTime.Now;
        }

        public ActivatedAlarm(int alarmId, string tagName, string message, double value)
        {
            AlarmId = alarmId;
            TagName = tagName;
            Message = message;
            Value = value;
            ActivationTime = DateTime.Now;
        }
    }
}
