using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class AnalogOutputTag : OutputTag
    {
        private double _lowLimit;
        private double _highLimit;
        private string _units;

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

        public AnalogOutputTag() : base()
        {
            TagType = TagType.AO;
        }

        public AnalogOutputTag(string tagName, string description, string ioAddress, 
            double initialValue, double lowLimit, double highLimit, string units)
            : base(TagType.AO, tagName, description, ioAddress, initialValue)
        {
            LowLimit = lowLimit;
            HighLimit = highLimit;
            Units = units;
        }
    }
}
