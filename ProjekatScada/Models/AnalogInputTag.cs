using ProjekatScada.Models.Enums;
using System.Collections.Generic;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class AnalogInputTag : InputTag
    {
        private double _lowLimit;
        private double _highLimit;
        private string _units;
        private double _deadband;
        private double _hysteresis;

        public double LowLimit
        {
            get { return _lowLimit; }
            set { SetProperty(ref _lowLimit, value); }
        }

        public double HighLimit
        {
            get { return _highLimit; }
            set { SetProperty(ref _highLimit, value); }
        }

        public string Units
        {
            get { return _units; }
            set { SetProperty(ref _units, value); }
        }

        public double Deadband
        {
            get { return _deadband; }
            set { SetProperty(ref _deadband, value); }
        }

        public double Hysteresis
        {
            get { return _hysteresis; }
            set { SetProperty(ref _hysteresis, value); }
        }

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
