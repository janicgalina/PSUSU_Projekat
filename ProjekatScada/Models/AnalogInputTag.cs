using ProjekatScada.Models.Enums;
using System.Collections.Generic;

namespace ProjekatScada.Models
{
    public class AnalogInputTag : InputTag
    {
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; }
        public double Deadband { get; set; }
        public double Hysteresis { get; set; }

        public virtual ICollection<Alarm> Alarms { get; set; }

        public AnalogInputTag() : base()
        {
            TagType = TagType.AI;
            Alarms = new List<Alarm>();
        }

        public AnalogInputTag(string tagName, string description, string ioAddress, 
            int scanTime, bool onOffScan, double lowLimit, double highLimit, 
            string units, double deadband, double hysteresis)
            : base(TagType.AI, tagName, description, ioAddress, scanTime, onOffScan)
        {
            LowLimit = lowLimit;
            HighLimit = highLimit;
            Units = units;
            Deadband = deadband;
            Hysteresis = hysteresis;
            Alarms = new List<Alarm>();
        }
    }
}
