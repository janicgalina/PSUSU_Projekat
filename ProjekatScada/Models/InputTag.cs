using ProjekatScada.Models.Enums;

using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public abstract class InputTag : TagBase
    {
        private int _scanTime;
        private bool _onOffScan;

        public int ScanTime
        {
            get { return _scanTime; }
            set { SetProperty(ref _scanTime, value); }
        }

        public bool OnOffScan
        {
            get { return _onOffScan; }
            set { SetProperty(ref _onOffScan, value); }
        }

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
