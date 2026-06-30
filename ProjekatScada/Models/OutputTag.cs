using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public abstract class OutputTag : TagBase
    {
        private double _initialValue;

        public double InitialValue
        {
            get { return _initialValue; }
            set { SetProperty(ref _initialValue, value); }
        }

        protected OutputTag() : base()
        {
        }

        protected OutputTag(TagType tagType, string tagName, string description, 
            string ioAddress, double initialValue)
            : base(tagType, tagName, description, ioAddress)
        {
            InitialValue = initialValue;
        }
    }
}
