using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class AnalogOutputTag : OutputTag
    {
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; }

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
