using ProjekatScada.Infrastructure;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class Alarm : ObservableObject
    {
        private int _id;
        private double _threshold;
        private AlarmTriggerType _triggerType;
        private string _message;
        private AlarmState _state;
        private int _analogInputTagId;
        private AnalogInputTag _analogInputTag;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public double Threshold
        {
            get { return _threshold; }
            set { SetProperty(ref _threshold, value); }
        }

        public AlarmTriggerType TriggerType
        {
            get { return _triggerType; }
            set { SetProperty(ref _triggerType, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public AlarmState State
        {
            get { return _state; }
            set
            {
                if (SetProperty(ref _state, value))
                {
                    OnPropertyChanged(nameof(IsAcknowledged));
                }
            }
        }

        public bool IsAcknowledged
        {
            get { return State == AlarmState.Acknowledged; }
        }

        public int AnalogInputTagId
        {
            get { return _analogInputTagId; }
            set { SetProperty(ref _analogInputTagId, value); }
        }

        public virtual AnalogInputTag AnalogInputTag
        {
            get { return _analogInputTag; }
            set { SetProperty(ref _analogInputTag, value); }
        }

        public Alarm()
        {
            State = AlarmState.Inactive;
        }

        public Alarm(double threshold, AlarmTriggerType triggerType, string message, int analogInputTagId)
        {
            Threshold = threshold;
            TriggerType = triggerType;
            Message = message;
            AnalogInputTagId = analogInputTagId;
            State = AlarmState.Inactive;
        }
    }
}
