using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class DigitalInputTag : InputTag
    {
        public DigitalInputTag() : base()
        {
            TagType = TagType.DI;
        }

        public DigitalInputTag(string tagName, string description, string ioAddress, 
            int scanTime, bool onOffScan)
            : base(TagType.DI, tagName, description, ioAddress, scanTime, onOffScan)
        {
        }
    }
}
