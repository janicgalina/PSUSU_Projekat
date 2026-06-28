using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public abstract class OutputTag : TagBase
    {
        public double InitialValue { get; set; }

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
