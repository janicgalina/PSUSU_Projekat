using ProjekatScada.Models.Enums;
using System.Collections.Generic;

namespace ProjekatScada.Models
{
    public abstract class TagBase
    {
        public int Id { get; set; }
        public TagType TagType { get; set; }
        public string TagName { get; set; }
        public string Description { get; set; }
        public string IOAddress { get; set; }

        protected TagBase()
        {
        }

        protected TagBase(TagType tagType, string tagName, string description, string ioAddress)
        {
            TagType = tagType;
            TagName = tagName;
            Description = description;
            IOAddress = ioAddress;
        }
    }
}
