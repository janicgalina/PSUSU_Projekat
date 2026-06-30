using ProjekatScada.Models.Enums;

namespace ProjekatScada.Data.Entities
{
    public class AlarmEntity
    {
        public int Id { get; set; }
        public double Threshold { get; set; }
        public AlarmTriggerType TriggerType { get; set; }
        public string Message { get; set; }
        public AlarmState State { get; set; }
        public int AnalogInputTagId { get; set; }
    }
}
