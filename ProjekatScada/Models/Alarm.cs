using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class Alarm
    {
        public int Id { get; set; }
        public double Threshold { get; set; }
        public AlarmTriggerType TriggerType { get; set; }
        public string Message { get; set; }
        public AlarmState State { get; set; }

        public int AnalogInputTagId { get; set; }
        public virtual AnalogInputTag AnalogInputTag { get; set; }

        public Alarm()
        {
            State = AlarmState.Inactive;
        }

        public Alarm(double threshold, AlarmTriggerType triggerType, string message, int analogInputTagId)
        {
            Threshold = threshold;
            TriggerType = triggerType;
            Message = message;
            AnalogInputTagId = analogInputTagId;
            State = AlarmState.Inactive;
        }
    }
}
