using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProjekatScada.Infrastructure;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.ViewModels
{
    public class AddItemViewModel : ObservableObject
    {
        private readonly IDataConcentratorService _dataConcentratorService;
        private readonly ITagValidationService _validationService;
        private string _selectedItemType;
        private string _tagName;
        private string _description;
        private string _ioAddress;
        private int _scanTime = 1000;
        private bool _onOffScan = true;
        private double _lowLimit;
        private double _highLimit = 100;
        private string _units;
        private double _deadband = 0.5;
        private double _hysteresis = 1.0;
        private double _initialValue;
        private double _alarmThreshold = 50;
        private AlarmTriggerType _alarmTriggerType = AlarmTriggerType.AboveLimit;
        private string _alarmMessage;
        private AnalogInputTag _selectedAnalogInputTag;
        private string _validationMessage;

        public AddItemViewModel(IDataConcentratorService dataConcentratorService, ITagValidationService validationService)
        {
            _dataConcentratorService = dataConcentratorService;
            _validationService = validationService;

            ItemTypes = new ObservableCollection<string> { "AI", "AO", "DI", "DO", "Alarm" };
            AlarmTriggerTypes = new ObservableCollection<AlarmTriggerType>
            {
                AlarmTriggerType.AboveLimit,
                AlarmTriggerType.BelowLimit
            };
            AnalogInputTags = new ObservableCollection<AnalogInputTag>(
                _dataConcentratorService.Tags.OfType<AnalogInputTag>());

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());

            SelectedItemType = "AI";
        }

        public ObservableCollection<string> ItemTypes { get; private set; }
        public ObservableCollection<AlarmTriggerType> AlarmTriggerTypes { get; private set; }
        public ObservableCollection<AnalogInputTag> AnalogInputTags { get; private set; }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public bool DialogResult { get; private set; }

        public string SelectedItemType
        {
            get { return _selectedItemType; }
            set
            {
                if (SetProperty(ref _selectedItemType, value))
                {
                    OnPropertyChanged(nameof(IsAnalogInput));
                    OnPropertyChanged(nameof(IsAnalogOutput));
                    OnPropertyChanged(nameof(IsDigitalInput));
                    OnPropertyChanged(nameof(IsDigitalOutput));
                    OnPropertyChanged(nameof(IsAlarm));
                    OnPropertyChanged(nameof(IsInputTag));
                    OnPropertyChanged(nameof(IsOutputTag));
                    OnPropertyChanged(nameof(IsAnalogTag));
                    OnPropertyChanged(nameof(IsTag));
                    ValidationMessage = string.Empty;
                }
            }
        }

        public bool IsAnalogInput { get { return SelectedItemType == "AI"; } }
        public bool IsAnalogOutput { get { return SelectedItemType == "AO"; } }
        public bool IsDigitalInput { get { return SelectedItemType == "DI"; } }
        public bool IsDigitalOutput { get { return SelectedItemType == "DO"; } }
        public bool IsAlarm { get { return SelectedItemType == "Alarm"; } }
        public bool IsInputTag { get { return IsAnalogInput || IsDigitalInput; } }
        public bool IsOutputTag { get { return IsAnalogOutput || IsDigitalOutput; } }
        public bool IsAnalogTag { get { return IsAnalogInput || IsAnalogOutput; } }
        public bool IsTag { get { return !IsAlarm; } }

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

        public double LowLimit
        {
            get { return _lowLimit; }
            set { SetProperty(ref _lowLimit, value); }
        }

        public double HighLimit
        {
            get { return _highLimit; }
            set { SetProperty(ref _highLimit, value); }
        }

        public string Units
        {
            get { return _units; }
            set { SetProperty(ref _units, value); }
        }

        public double Deadband
        {
            get { return _deadband; }
            set { SetProperty(ref _deadband, value); }
        }

        public double Hysteresis
        {
            get { return _hysteresis; }
            set { SetProperty(ref _hysteresis, value); }
        }

        public double InitialValue
        {
            get { return _initialValue; }
            set { SetProperty(ref _initialValue, value); }
        }

        public double AlarmThreshold
        {
            get { return _alarmThreshold; }
            set { SetProperty(ref _alarmThreshold, value); }
        }

        public AlarmTriggerType AlarmTriggerType
        {
            get { return _alarmTriggerType; }
            set { SetProperty(ref _alarmTriggerType, value); }
        }

        public string AlarmMessage
        {
            get { return _alarmMessage; }
            set { SetProperty(ref _alarmMessage, value); }
        }

        public AnalogInputTag SelectedAnalogInputTag
        {
            get { return _selectedAnalogInputTag; }
            set { SetProperty(ref _selectedAnalogInputTag, value); }
        }

        public string ValidationMessage
        {
            get { return _validationMessage; }
            set { SetProperty(ref _validationMessage, value); }
        }

        private bool CanSave()
        {
            if (IsAlarm)
            {
                return SelectedAnalogInputTag != null &&
                       !string.IsNullOrWhiteSpace(AlarmMessage);
            }

            return !string.IsNullOrWhiteSpace(TagName) &&
                   !string.IsNullOrWhiteSpace(Description) &&
                   !string.IsNullOrWhiteSpace(IOAddress);
        }

        private void Save()
        {
            try
            {
                if (IsAlarm)
                {
                    SaveAlarm();
                }
                else
                {
                    SaveTag();
                }

                DialogResult = true;
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ValidationMessage = ex.Message;
            }
        }

        private void SaveTag()
        {
            TagBase tag = null;

            if (IsAnalogInput)
            {
                tag = new AnalogInputTag(TagName, Description, IOAddress, ScanTime, OnOffScan,
                    LowLimit, HighLimit, Units, Deadband, Hysteresis);
            }
            else if (IsAnalogOutput)
            {
                tag = new AnalogOutputTag(TagName, Description, IOAddress, InitialValue,
                    LowLimit, HighLimit, Units);
            }
            else if (IsDigitalInput)
            {
                tag = new DigitalInputTag(TagName, Description, IOAddress, ScanTime, OnOffScan);
            }
            else if (IsDigitalOutput)
            {
                tag = new DigitalOutputTag(TagName, Description, IOAddress, InitialValue);
            }

            if (tag != null)
            {
                var validationErrors = _validationService.ValidateTag(tag, _dataConcentratorService.Tags).ToList();
                if (validationErrors.Any())
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
                }

                _dataConcentratorService.AddTag(tag);
            }
        }

        private void SaveAlarm()
        {
            if (SelectedAnalogInputTag == null)
            {
                throw new InvalidOperationException("Morate izabrati AI tag za alarm.");
            }

            var alarm = new Alarm(AlarmThreshold, AlarmTriggerType, AlarmMessage, SelectedAnalogInputTag.Id);
            var validationErrors = _validationService.ValidateAlarm(alarm, SelectedAnalogInputTag, _dataConcentratorService.Alarms).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            _dataConcentratorService.AddAlarm(alarm);
        }

        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RequestClose;
    }
}
