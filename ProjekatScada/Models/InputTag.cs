using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public abstract class InputTag : TagBase
    {
        public int ScanTime { get; set; }
        public bool OnOffScan { get; set; }

        protected InputTag() : base()
        {
        }

        protected InputTag(TagType tagType, string tagName, string description, 
            string ioAddress, int scanTime, bool onOffScan)
            : base(tagType, tagName, description, ioAddress)
        {
            ScanTime = scanTime;
            OnOffScan = onOffScan;
        }
    }
}
