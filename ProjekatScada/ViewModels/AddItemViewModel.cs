using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
        private readonly TagBase _tagToEdit;
        private readonly Alarm _alarmToEdit;
        private string _selectedItemType;
        private string _tagName;
        private string _description;
        private string _ioAddress;
        private int _scanTime = 1000;
        private bool _onOffScan = true;
        private double _lowLimit;
        private double _highLimit = 100;
        private string _units = "°C";
        private double _deadband = 0.5;
        private double _hysteresis = 1.0;
        private double _initialValue;
        private double _alarmThreshold = 50;
        private string _lowLimitText = "0";
        private string _highLimitText = "100";
        private string _deadbandText = "0.5";
        private string _hysteresisText = "1.0";
        private string _scanTimeText = "1000";
        private string _initialValueText = "0";
        private string _alarmThresholdText = "50";
        private AlarmTriggerType _alarmTriggerType = AlarmTriggerType.AboveLimit;
        private string _alarmMessage;
        private AnalogInputTag _selectedAnalogInputTag;
        private string _validationMessage;

        public AddItemViewModel(
            IDataConcentratorService dataConcentratorService,
            ITagValidationService validationService,
            TagBase tagToEdit = null,
            Alarm alarmToEdit = null)
        {
            _dataConcentratorService = dataConcentratorService;
            _validationService = validationService;
            _tagToEdit = tagToEdit;
            _alarmToEdit = alarmToEdit;
            IsEditMode = tagToEdit != null || alarmToEdit != null;

            ItemTypes = new ObservableCollection<string> { "AI", "AO", "DI", "DO", "Alarm" };
            AlarmTriggerTypes = new ObservableCollection<AlarmTriggerType>
            {
                AlarmTriggerType.AboveLimit,
                AlarmTriggerType.BelowLimit
            };
            AnalogInputTags = new ObservableCollection<AnalogInputTag>(
                _dataConcentratorService.Tags.OfType<AnalogInputTag>());
            UnitOptions = new ObservableCollection<string>
            {
                "°C", "°F", "K", "%", "bar", "Pa", "kPa", "MPa",
                "L/min", "m³/h", "rpm", "V", "A", "W", "kW",
                "kg", "kg/h", "mm", "cm", "m", "s", "min", "h", "Hz", "ppm"
            };

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());

            if (_alarmToEdit != null)
            {
                InitializeSelectedItemType("Alarm");
                AlarmThreshold = _alarmToEdit.Threshold;
                AlarmTriggerType = _alarmToEdit.TriggerType;
                AlarmMessage = _alarmToEdit.Message;
                SelectedAnalogInputTag = AnalogInputTags.FirstOrDefault(t => t.Id == _alarmToEdit.AnalogInputTagId);
                SyncNumericTextFields();
            }
            else if (_tagToEdit != null)
            {
                LoadTagForEdit(_tagToEdit);
            }
            else
            {
                InitializeSelectedItemType("AI");
            }

            NotifySaveStateChanged();
        }

        public bool IsEditMode { get; private set; }
        public string WindowTitle
        {
            get { return IsEditMode ? "Izmeni tag ili alarm" : "Dodaj tag ili alarm"; }
        }

        public ObservableCollection<string> ItemTypes { get; private set; }
        public ObservableCollection<AlarmTriggerType> AlarmTriggerTypes { get; private set; }
        public ObservableCollection<AnalogInputTag> AnalogInputTags { get; private set; }
        public ObservableCollection<string> UnitOptions { get; private set; }

        public RelayCommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public bool DialogResult { get; private set; }

        public string SelectedItemType
        {
            get { return _selectedItemType; }
            set
            {
                if (IsEditMode)
                {
                    return;
                }

                if (SetProperty(ref _selectedItemType, value))
                {
                    NotifyTypePropertiesChanged();
                    ValidationMessage = string.Empty;
                    NotifySaveStateChanged();
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
        public bool CanChangeItemType { get { return !IsEditMode; } }

        public string TagName
        {
            get { return _tagName; }
            set { SetFormField(ref _tagName, value); }
        }

        public string Description
        {
            get { return _description; }
            set { SetFormField(ref _description, value); }
        }

        public string IOAddress
        {
            get { return _ioAddress; }
            set { SetFormField(ref _ioAddress, value); }
        }

        public int ScanTime
        {
            get { return _scanTime; }
            set { SetFormField(ref _scanTime, value); }
        }

        public string ScanTimeText
        {
            get { return _scanTimeText; }
            set { SetIntegerText(ref _scanTimeText, value, ref _scanTime); }
        }

        public bool OnOffScan
        {
            get { return _onOffScan; }
            set { SetFormField(ref _onOffScan, value); }
        }

        public double LowLimit
        {
            get { return _lowLimit; }
            set { SetFormField(ref _lowLimit, value); }
        }

        public string LowLimitText
        {
            get { return _lowLimitText; }
            set { SetDoubleText(ref _lowLimitText, value, ref _lowLimit); }
        }

        public double HighLimit
        {
            get { return _highLimit; }
            set { SetFormField(ref _highLimit, value); }
        }

        public string HighLimitText
        {
            get { return _highLimitText; }
            set { SetDoubleText(ref _highLimitText, value, ref _highLimit); }
        }

        public string Units
        {
            get { return _units; }
            set { SetFormField(ref _units, value); }
        }

        public double Deadband
        {
            get { return _deadband; }
            set { SetFormField(ref _deadband, value); }
        }

        public string DeadbandText
        {
            get { return _deadbandText; }
            set { SetDoubleText(ref _deadbandText, value, ref _deadband, allowNegative: false); }
        }

        public double Hysteresis
        {
            get { return _hysteresis; }
            set { SetFormField(ref _hysteresis, value); }
        }

        public string HysteresisText
        {
            get { return _hysteresisText; }
            set { SetDoubleText(ref _hysteresisText, value, ref _hysteresis, allowNegative: false); }
        }

        public double InitialValue
        {
            get { return _initialValue; }
            set { SetFormField(ref _initialValue, value); }
        }

        public string InitialValueText
        {
            get { return _initialValueText; }
            set { SetDoubleText(ref _initialValueText, value, ref _initialValue); }
        }

        public double AlarmThreshold
        {
            get { return _alarmThreshold; }
            set { SetFormField(ref _alarmThreshold, value); }
        }

        public string AlarmThresholdText
        {
            get { return _alarmThresholdText; }
            set { SetDoubleText(ref _alarmThresholdText, value, ref _alarmThreshold); }
        }

        public AlarmTriggerType AlarmTriggerType
        {
            get { return _alarmTriggerType; }
            set { SetFormField(ref _alarmTriggerType, value); }
        }

        public string AlarmMessage
        {
            get { return _alarmMessage; }
            set { SetFormField(ref _alarmMessage, value); }
        }

        public AnalogInputTag SelectedAnalogInputTag
        {
            get { return _selectedAnalogInputTag; }
            set { SetFormField(ref _selectedAnalogInputTag, value); }
        }

        public string ValidationMessage
        {
            get { return _validationMessage; }
            set { SetProperty(ref _validationMessage, value); }
        }

        public string SaveRequirementsHint
        {
            get
            {
                var missing = GetMissingRequirements();
                if (missing.Count == 0)
                {
                    return string.Empty;
                }

                return "Za čuvanje popunite: " + string.Join(", ", missing) + ".";
            }
        }

        private bool CanSave()
        {
            return GetMissingRequirements().Count == 0;
        }

        private List<string> GetMissingRequirements()
        {
            var missing = new List<string>();

            if (IsAlarm)
            {
                if (SelectedAnalogInputTag == null)
                {
                    missing.Add("AI tag");
                }

                if (string.IsNullOrWhiteSpace(AlarmMessage))
                {
                    missing.Add("poruku alarma");
                }

                if (!NumericInputHelper.TryParseDouble(AlarmThresholdText, out _))
                {
                    missing.Add("ispravan threshold");
                }

                return missing;
            }

            if (string.IsNullOrWhiteSpace(TagName))
            {
                missing.Add("tag name");
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                missing.Add("description");
            }

            if (string.IsNullOrWhiteSpace(IOAddress))
            {
                missing.Add("I/O address");
            }

            if (IsInputTag && ScanTime <= 0)
            {
                missing.Add("scan time veći od 0");
            }

            if (IsInputTag && !NumericInputHelper.TryParseInt(ScanTimeText, out _))
            {
                missing.Add("ispravan scan time");
            }

            if (IsAnalogTag)
            {
                if (string.IsNullOrWhiteSpace(Units))
                {
                    missing.Add("units");
                }

                if (!TryCommitNumericFields(out var numericError))
                {
                    missing.Add(numericError);
                }
                else if (LowLimit >= HighLimit)
                {
                    missing.Add("low limit manji od high limit");
                }
            }

            if (IsAnalogOutput && (InitialValue < LowLimit || InitialValue > HighLimit))
            {
                missing.Add("initial value unutar granica");
            }

            if (IsDigitalOutput)
            {
                double initialValue;
                if (!NumericInputHelper.TryParseDouble(InitialValueText, out initialValue))
                {
                    missing.Add("ispravna initial value (0 ili 1)");
                }
                else if (initialValue != 0d && initialValue != 1d)
                {
                    missing.Add("initial value 0 ili 1");
                }
            }

            return missing;
        }

        private bool SetFormField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (!SetProperty(ref field, value, propertyName))
            {
                return false;
            }

            ValidationMessage = string.Empty;
            NotifySaveStateChanged();
            return true;
        }

        private void SetDoubleText(
            ref string textField,
            string value,
            ref double numericField,
            bool allowNegative = true,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            value = value ?? string.Empty;
            if (!NumericInputHelper.IsValidNumericText(value, allowDecimal: true, allowNegative: allowNegative))
            {
                OnPropertyChanged(propertyName);
                return;
            }

            if (!SetProperty(ref textField, value, propertyName))
            {
                return;
            }

            double parsed;
            if (NumericInputHelper.TryParseDouble(value, out parsed))
            {
                numericField = parsed;
            }

            ValidationMessage = string.Empty;
            NotifySaveStateChanged();
        }

        private void SetIntegerText(
            ref string textField,
            string value,
            ref int numericField,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            value = value ?? string.Empty;
            if (!NumericInputHelper.IsValidNumericText(value, allowDecimal: false, allowNegative: false))
            {
                OnPropertyChanged(propertyName);
                return;
            }

            if (!SetProperty(ref textField, value, propertyName))
            {
                return;
            }

            int parsed;
            if (NumericInputHelper.TryParseInt(value, out parsed))
            {
                numericField = parsed;
            }

            ValidationMessage = string.Empty;
            NotifySaveStateChanged();
        }

        private bool TryCommitNumericFields(out string error)
        {
            error = null;

            if (!NumericInputHelper.TryParseDouble(LowLimitText, out _lowLimit))
            {
                error = "ispravan low limit";
                return false;
            }

            if (!NumericInputHelper.TryParseDouble(HighLimitText, out _highLimit))
            {
                error = "ispravan high limit";
                return false;
            }

            if (IsAnalogInput)
            {
                if (!NumericInputHelper.TryParseDouble(DeadbandText, out _deadband))
                {
                    error = "ispravan deadband";
                    return false;
                }

                if (!NumericInputHelper.TryParseDouble(HysteresisText, out _hysteresis))
                {
                    error = "ispravan hysteresis";
                    return false;
                }
            }

            if (IsOutputTag && !NumericInputHelper.TryParseDouble(InitialValueText, out _initialValue))
            {
                error = "ispravna initial value";
                return false;
            }

            return true;
        }

        private void SyncNumericTextFields()
        {
            _lowLimitText = _lowLimit.ToString(CultureInfo.InvariantCulture);
            _highLimitText = _highLimit.ToString(CultureInfo.InvariantCulture);
            _deadbandText = _deadband.ToString(CultureInfo.InvariantCulture);
            _hysteresisText = _hysteresis.ToString(CultureInfo.InvariantCulture);
            _scanTimeText = _scanTime.ToString(CultureInfo.InvariantCulture);
            _initialValueText = _initialValue.ToString(CultureInfo.InvariantCulture);
            _alarmThresholdText = _alarmThreshold.ToString(CultureInfo.InvariantCulture);

            OnPropertyChanged(nameof(LowLimitText));
            OnPropertyChanged(nameof(HighLimitText));
            OnPropertyChanged(nameof(DeadbandText));
            OnPropertyChanged(nameof(HysteresisText));
            OnPropertyChanged(nameof(ScanTimeText));
            OnPropertyChanged(nameof(InitialValueText));
            OnPropertyChanged(nameof(AlarmThresholdText));
        }

        private void EnsureUnitOptionExists(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit) || UnitOptions.Contains(unit))
            {
                return;
            }

            UnitOptions.Add(unit);
        }

        private void NotifySaveStateChanged()
        {
            SaveCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SaveRequirementsHint));
        }

        private void Save()
        {
            try
            {
                if (!TryCommitNumericFields(out var numericError))
                {
                    throw new InvalidOperationException("Popunite numerička polja ispravnim vrednostima: " + numericError + ".");
                }

                if (IsInputTag && !NumericInputHelper.TryParseInt(ScanTimeText, out _scanTime))
                {
                    throw new InvalidOperationException("Scan time mora biti ceo broj veći od 0.");
                }

                if (IsAlarm && !NumericInputHelper.TryParseDouble(AlarmThresholdText, out _alarmThreshold))
                {
                    throw new InvalidOperationException("Threshold mora biti ispravan broj.");
                }

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
            TagBase tag = BuildTagFromForm();
            if (tag == null)
            {
                throw new InvalidOperationException("Tip taga nije prepoznat. Zatvorite prozor i pokušajte ponovo.");
            }

            var validationErrors = _validationService.ValidateTag(tag, _dataConcentratorService.Tags).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            if (_tagToEdit != null)
            {
                _dataConcentratorService.UpdateTag(tag);
            }
            else
            {
                _dataConcentratorService.AddTag(tag);
            }
        }

        private void SaveAlarm()
        {
            if (SelectedAnalogInputTag == null)
            {
                throw new InvalidOperationException("Morate izabrati AI tag za alarm.");
            }

            Alarm alarm;
            if (_alarmToEdit != null)
            {
                alarm = _alarmToEdit;
                alarm.Threshold = AlarmThreshold;
                alarm.TriggerType = AlarmTriggerType;
                alarm.Message = AlarmMessage;
                alarm.AnalogInputTagId = SelectedAnalogInputTag.Id;
            }
            else
            {
                alarm = new Alarm(AlarmThreshold, AlarmTriggerType, AlarmMessage, SelectedAnalogInputTag.Id);
            }

            var validationErrors = _validationService.ValidateAlarm(alarm, SelectedAnalogInputTag, _dataConcentratorService.Alarms).ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            if (_alarmToEdit != null)
            {
                _dataConcentratorService.UpdateAlarm(alarm);
            }
            else
            {
                _dataConcentratorService.AddAlarm(alarm);
            }
        }

        private TagBase BuildTagFromForm()
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

            if (tag != null && _tagToEdit != null)
            {
                tag.Id = _tagToEdit.Id;
            }

            return tag;
        }

        private void LoadTagForEdit(TagBase tag)
        {
            InitializeSelectedItemType(tag.TagType.ToString());
            TagName = tag.TagName;
            Description = tag.Description;
            IOAddress = tag.IOAddress;

            var inputTag = tag as InputTag;
            if (inputTag != null)
            {
                ScanTime = inputTag.ScanTime;
                OnOffScan = inputTag.OnOffScan;
            }

            var analogInputTag = tag as AnalogInputTag;
            if (analogInputTag != null)
            {
                LowLimit = analogInputTag.LowLimit;
                HighLimit = analogInputTag.HighLimit;
                Units = analogInputTag.Units;
                Deadband = analogInputTag.Deadband;
                Hysteresis = analogInputTag.Hysteresis;
            }

            var analogOutputTag = tag as AnalogOutputTag;
            if (analogOutputTag != null)
            {
                LowLimit = analogOutputTag.LowLimit;
                HighLimit = analogOutputTag.HighLimit;
                Units = analogOutputTag.Units;
                InitialValue = analogOutputTag.InitialValue;
            }

            var digitalOutputTag = tag as DigitalOutputTag;
            if (digitalOutputTag != null)
            {
                InitialValue = digitalOutputTag.InitialValue;
            }

            EnsureUnitOptionExists(Units);
            SyncNumericTextFields();
        }

        private void NotifyTypePropertiesChanged()
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
        }

        private void InitializeSelectedItemType(string itemType)
        {
            if (SetProperty(ref _selectedItemType, itemType))
            {
                NotifyTypePropertiesChanged();
                ValidationMessage = string.Empty;
                NotifySaveStateChanged();
            }
        }

        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RequestClose;
    }
}
