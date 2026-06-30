using System;
using ProjekatScada.Infrastructure;
using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class ActivatedAlarm : ObservableObject
    {
        private int _id;
        private int _alarmId;
        private string _tagName;
        private string _message;
        private DateTime _activationTime;
        private double _value;
        private AlarmState _state;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public int AlarmId
        {
            get { return _alarmId; }
            set { SetProperty(ref _alarmId, value); }
        }

        public string TagName
        {
            get { return _tagName; }
            set { SetProperty(ref _tagName, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public DateTime ActivationTime
        {
            get { return _activationTime; }
            set { SetProperty(ref _activationTime, value); }
        }

        public double Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        public AlarmState State
        {
            get { return _state; }
            set { SetProperty(ref _state, value); }
        }

        public ActivatedAlarm()
        {
            ActivationTime = DateTime.Now;
            State = AlarmState.Active;
        }

        public ActivatedAlarm(int alarmId, string tagName, string message, double value)
        {
            AlarmId = alarmId;
            TagName = tagName;
            Message = message;
            Value = value;
            ActivationTime = DateTime.Now;
            State = AlarmState.Active;
        }
    }
}
