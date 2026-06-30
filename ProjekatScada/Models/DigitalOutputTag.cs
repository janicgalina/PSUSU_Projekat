using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class DigitalOutputTag : OutputTag
    {
        public DigitalOutputTag() : base()
        {
            TagType = TagType.DO;
        }

        public DigitalOutputTag(string tagName, string description, string ioAddress, 
            double initialValue)
            : base(TagType.DO, tagName, description, ioAddress, initialValue)
        {
        }
    }
}
