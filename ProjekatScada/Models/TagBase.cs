using ProjekatScada.Infrastructure;
using ProjekatScada.Models.Enums;
using System;

namespace ProjekatScada.Models
{
    public abstract class TagBase : ObservableObject
    {
        private int _id;
        private TagType _tagType;
        private string _tagName;
        private string _description;
        private string _ioAddress;
        private double _currentValue;
        private DateTime _lastUpdated;
        private bool _isInAlarm;
        private bool _hasUnacknowledgedAlarm;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public TagType TagType
        {
            get { return _tagType; }
            set { SetProperty(ref _tagType, value); }
        }

        public string TagName
        {
            get { return _tagName; }
            set { SetProperty(ref _tagName, value); }
        }

        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        public string IOAddress
        {
            get { return _ioAddress; }
            set { SetProperty(ref _ioAddress, value); }
        }

        public double CurrentValue
        {
            get { return _currentValue; }
            set { SetProperty(ref _currentValue, value); }
        }

        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetProperty(ref _lastUpdated, value); }
        }

        public bool IsInAlarm
        {
            get { return _isInAlarm; }
            set { SetProperty(ref _isInAlarm, value); }
        }

        public bool HasUnacknowledgedAlarm
        {
            get { return _hasUnacknowledgedAlarm; }
            set { SetProperty(ref _hasUnacknowledgedAlarm, value); }
        }

        protected TagBase()
        {
            LastUpdated = DateTime.MinValue;
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
