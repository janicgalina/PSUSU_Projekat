using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using ProjekatScada.Infrastructure;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;
using ProjekatScada.Properties;
using ProjekatScada.Services;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private IDataConcentratorService _dataConcentratorService;
        private ITagValidationService _validationService;
        private IPlcSimulator _plcSimulator;
        private ISystemLogger _logger;
        private IAlarmSoundService _alarmSoundService;
        private readonly Random _random = new Random();
        private DispatcherTimer _scanTimer;
        private TagBase _selectedTag;
        private Alarm _selectedAlarm;
        private ActivatedAlarm _selectedActivatedAlarm;
        private string _statusMessage;
        private string _currentUser;
        private UserSession _session;
        private bool _canWrite;
        private double _alarmVolumePercent = 70;
        private AlarmSoundOption _selectedAlarmSoundOption;
        private ThemeOption _selectedThemeOption;
        private bool _isAlarmSoundActive;
        private bool _isShutdown;

        public MainViewModel()
        {
            var services = CreateDefaultServices();
            Initialize(services.Item1, services.Item2, services.Item3);
        }

        public MainViewModel(IPlcSimulator plcSimulator, IDataConcentratorService dataConcentratorService, ISystemLogger logger)
        {
            Initialize(plcSimulator, dataConcentratorService, logger);
        }

        private void Initialize(IPlcSimulator plcSimulator, IDataConcentratorService dataConcentratorService, ISystemLogger logger)
        {
            _plcSimulator = plcSimulator;
            _dataConcentratorService = dataConcentratorService;
            _logger = logger;
            _alarmSoundService = new AlarmSoundService();
            _validationService = new TagValidationService();

            Tags = new ObservableCollection<TagBase>();
            Alarms = new ObservableCollection<Alarm>();
            ActivatedAlarms = new ObservableCollection<ActivatedAlarm>();
            ActivityFeed = new ObservableCollection<string>();
            AlarmSoundOptions = new ObservableCollection<AlarmSoundOption>
            {
                new AlarmSoundOption { Profile = AlarmSoundProfile.ClassicBeep, DisplayName = "Klasični beep" },
                new AlarmSoundOption { Profile = AlarmSoundProfile.Siren, DisplayName = "Sirena" },
                new AlarmSoundOption { Profile = AlarmSoundProfile.Buzzer, DisplayName = "Buzzer" },
                new AlarmSoundOption { Profile = AlarmSoundProfile.AlertPulse, DisplayName = "Alert pulse" }
            };
            ThemeOptions = new ObservableCollection<ThemeOption>
            {
                new ThemeOption { Theme = ApplicationTheme.Light, DisplayName = "Light" },
                new ThemeOption { Theme = ApplicationTheme.Dark, DisplayName = "Dark" }
            };

            LoadAppearanceSettings();

            LoadDemoDataCommand = new RelayCommand(_ => LoadDemoData(), _ => CanWrite);
            OpenAddWindowCommand = new RelayCommand(_ => OpenAddWindow(), _ => CanWrite);
            EditSelectedTagCommand = new RelayCommand(_ => OpenEditWindow(), _ => CanWrite && HasEditableSelection());
            ShowTagDetailsCommand = new RelayCommand(_ => ShowTagDetails(), _ => SelectedTag is AnalogInputTag);
            ScanCommand = new RelayCommand(_ => ExecuteSafely(ScanInputs));
            ToggleScanCommand = new RelayCommand(_ => ExecuteSafely(ToggleSelectedScan), _ => CanWrite && SelectedTag is InputTag);
            WriteOutputCommand = new RelayCommand(_ => ExecuteSafely(WriteSelectedOutputValue), _ => CanWrite && SelectedTag is OutputTag);
            AcknowledgeAlarmCommand = new RelayCommand(_ => ExecuteSafely(AcknowledgeSelectedAlarm), _ => CanWrite && HasAcknowledgeableSelection());
            GenerateReportCommand = new RelayCommand(_ => ExecuteSafely(GenerateReport));
            RemoveSelectedTagCommand = new RelayCommand(_ => ExecuteSafely(RemoveSelectedTag), _ => CanWrite && SelectedTag != null);
            RemoveSelectedAlarmCommand = new RelayCommand(_ => ExecuteSafely(RemoveSelectedAlarm), _ => CanWrite && SelectedAlarm != null);
            OpenTagValueSearchCommand = new RelayCommand(_ => OpenTagValueSearchWindow());
            PreviewAlarmSoundCommand = new RelayCommand(_ => ExecuteSafely(PreviewAlarmSound));
            LogoutCommand = new RelayCommand(_ => OnLogoutRequested());

            _dataConcentratorService.TagValueChanged += DataConcentratorService_TagValueChanged;
            _dataConcentratorService.AlarmRaised += DataConcentratorService_AlarmRaised;

            _scanTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _scanTimer.Tick += (s, e) => ExecuteSafely(AutoScanInputs);
            _scanTimer.Start();

            StatusMessage = "Spremno za rad SCADA sistema.";
        }

        public ObservableCollection<TagBase> Tags { get; private set; }
        public ObservableCollection<Alarm> Alarms { get; private set; }
        public ObservableCollection<ActivatedAlarm> ActivatedAlarms { get; private set; }
        public ObservableCollection<string> ActivityFeed { get; private set; }
        public ObservableCollection<AlarmSoundOption> AlarmSoundOptions { get; private set; }
        public ObservableCollection<ThemeOption> ThemeOptions { get; private set; }

        public RelayCommand LoadDemoDataCommand { get; private set; }
        public RelayCommand OpenAddWindowCommand { get; private set; }
        public RelayCommand EditSelectedTagCommand { get; private set; }
        public RelayCommand ShowTagDetailsCommand { get; private set; }
        public ICommand ScanCommand { get; private set; }
        public RelayCommand ToggleScanCommand { get; private set; }
        public RelayCommand WriteOutputCommand { get; private set; }
        public RelayCommand AcknowledgeAlarmCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public RelayCommand RemoveSelectedTagCommand { get; private set; }
        public RelayCommand RemoveSelectedAlarmCommand { get; private set; }
        public RelayCommand OpenTagValueSearchCommand { get; private set; }
        public RelayCommand PreviewAlarmSoundCommand { get; private set; }
        public RelayCommand LogoutCommand { get; private set; }

        public event EventHandler LogoutRequested;

        public TagBase SelectedTag
        {
            get { return _selectedTag; }
            set
            {
                if (SetProperty(ref _selectedTag, value))
                {
                    if (value != null)
                    {
                        ClearAlarmSelections();
                    }

                    RaiseSelectionCommandStates();
                }
            }
        }

        public Alarm SelectedAlarm
        {
            get { return _selectedAlarm; }
            set
            {
                if (SetProperty(ref _selectedAlarm, value))
                {
                    if (value != null)
                    {
                        ClearTagSelection();
                        ClearActivatedAlarmSelection();
                    }

                    RaiseSelectionCommandStates();
                }
            }
        }

        public ActivatedAlarm SelectedActivatedAlarm
        {
            get { return _selectedActivatedAlarm; }
            set
            {
                if (SetProperty(ref _selectedActivatedAlarm, value))
                {
                    if (value != null)
                    {
                        ClearTagSelection();
                        ClearAlarmDefinitionSelection();
                    }

                    RaiseSelectionCommandStates();
                }
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        public string CurrentUser
        {
            get { return _currentUser; }
            set { SetProperty(ref _currentUser, value); }
        }

        public bool CanWrite
        {
            get { return _canWrite; }
            private set { SetProperty(ref _canWrite, value); }
        }

        public string AccessModeText
        {
            get { return CanWrite ? "Read/Write" : "Read Only"; }
        }

        public string UserInfoText
        {
            get
            {
                return _session == null
                    ? string.Empty
                    : string.Format("{0} ({1}) - {2}", _session.Username, _session.RoleDisplayName, AccessModeText);
            }
        }

        public double AlarmVolumePercent
        {
            get { return _alarmVolumePercent; }
            set
            {
                if (SetProperty(ref _alarmVolumePercent, value))
                {
                    _alarmSoundService.Volume = value / 100d;
                    Settings.Default.AlarmVolume = _alarmSoundService.Volume;
                    Settings.Default.Save();
                }
            }
        }

        public AlarmSoundOption SelectedAlarmSoundOption
        {
            get { return _selectedAlarmSoundOption; }
            set
            {
                if (SetProperty(ref _selectedAlarmSoundOption, value) && value != null)
                {
                    _alarmSoundService.SelectedProfile = value.Profile;
                    Settings.Default.AlarmSoundProfile = value.Profile.ToString();
                    Settings.Default.Save();
                }
            }
        }

        public ThemeOption SelectedThemeOption
        {
            get { return _selectedThemeOption; }
            set
            {
                if (SetProperty(ref _selectedThemeOption, value) && value != null)
                {
                    ThemeService.ApplyTheme(value.Theme);
                }
            }
        }

        public IDataConcentratorService DataConcentratorService
        {
            get { return _dataConcentratorService; }
        }

        public ITagValidationService ValidationService
        {
            get { return _validationService; }
        }

        public void InitializeAfterLogin(UserSession session)
        {
            _session = session;
            CanWrite = session.CanWrite;
            CurrentUser = session.Username;
            OnPropertyChanged(nameof(AccessModeText));
            OnPropertyChanged(nameof(UserInfoText));
            RaiseWriteCommandStates();

            _logger.Log(string.Format("Login korisnika '{0}' u ulozi {1} ({2}).", session.Username, session.Role, AccessModeText));

            try
            {
                _dataConcentratorService.LoadFromRepository();
                SyncCollections();
                StatusMessage = CanWrite
                    ? string.Format("Dobrodošli, {0}. Imate Read/Write pristup.", session.Username)
                    : string.Format("Dobrodošli, {0}. Imate Read Only pristup.", session.Username);
            }
            catch (Exception ex)
            {
                _logger.Log(string.Format("ERROR | Greška pri učitavanju baze: {0}", ex.Message));
                StatusMessage = "Baza je prazna ili nedostupna. Možete učitati demo podatke.";
            }

            AppendActivity(StatusMessage);
        }

        public void Shutdown()
        {
            if (_isShutdown)
            {
                return;
            }

            _isShutdown = true;

            if (_scanTimer != null)
            {
                _scanTimer.Stop();
                _scanTimer = null;
            }

            _dataConcentratorService.TagValueChanged -= DataConcentratorService_TagValueChanged;
            _dataConcentratorService.AlarmRaised -= DataConcentratorService_AlarmRaised;

            _alarmSoundService.Stop();
            _isAlarmSoundActive = false;
        }

        private void EnsureCanWrite()
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException("Nemate dozvolu za izmenu podataka. Samo admin ima Write pristup.");
            }
        }

        private void RaiseWriteCommandStates()
        {
            LoadDemoDataCommand.RaiseCanExecuteChanged();
            OpenAddWindowCommand.RaiseCanExecuteChanged();
            EditSelectedTagCommand.RaiseCanExecuteChanged();
            ToggleScanCommand.RaiseCanExecuteChanged();
            WriteOutputCommand.RaiseCanExecuteChanged();
            AcknowledgeAlarmCommand.RaiseCanExecuteChanged();
            RemoveSelectedTagCommand.RaiseCanExecuteChanged();
            RemoveSelectedAlarmCommand.RaiseCanExecuteChanged();
            ShowTagDetailsCommand.RaiseCanExecuteChanged();
        }

        private void RaiseSelectionCommandStates()
        {
            EditSelectedTagCommand.RaiseCanExecuteChanged();
            ToggleScanCommand.RaiseCanExecuteChanged();
            WriteOutputCommand.RaiseCanExecuteChanged();
            AcknowledgeAlarmCommand.RaiseCanExecuteChanged();
            RemoveSelectedTagCommand.RaiseCanExecuteChanged();
            RemoveSelectedAlarmCommand.RaiseCanExecuteChanged();
            ShowTagDetailsCommand.RaiseCanExecuteChanged();
        }

        private bool HasEditableSelection()
        {
            return SelectedTag != null || SelectedAlarm != null;
        }

        private bool HasAcknowledgeableSelection()
        {
            if (SelectedActivatedAlarm != null)
            {
                return SelectedActivatedAlarm.State == AlarmState.Active;
            }

            if (SelectedAlarm != null)
            {
                return SelectedAlarm.State == AlarmState.Active;
            }

            return false;
        }

        private void ClearTagSelection()
        {
            if (_selectedTag != null)
            {
                _selectedTag = null;
                OnPropertyChanged(nameof(SelectedTag));
            }
        }

        private void ClearAlarmDefinitionSelection()
        {
            if (_selectedAlarm != null)
            {
                _selectedAlarm = null;
                OnPropertyChanged(nameof(SelectedAlarm));
            }
        }

        private void ClearActivatedAlarmSelection()
        {
            if (_selectedActivatedAlarm != null)
            {
                _selectedActivatedAlarm = null;
                OnPropertyChanged(nameof(SelectedActivatedAlarm));
            }
        }

        private void ClearAlarmSelections()
        {
            ClearAlarmDefinitionSelection();
            ClearActivatedAlarmSelection();
        }

        private void OnLogoutRequested()
        {
            var handler = LogoutRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void LoadDemoData()
        {
            EnsureCanWrite();
            if (Tags.Any())
            {
                StatusMessage = "Demo podaci se mogu učitati samo kada nema tagova.";
                return;
            }

            var analogInput = new AnalogInputTag("AI_Temp01", "Temperatura kotla", "AI_01", 1000, true, 0, 100, "°C", 0.5, 1.0);
            var digitalInput = new DigitalInputTag("DI_Pump01", "Status pumpe", "DI_01", 1000, true);
            var analogOutput = new AnalogOutputTag("AO_Valve01", "Otvor ventila", "AO_01", 25, 0, 100, "%");
            var digitalOutput = new DigitalOutputTag("DO_AlarmHorn", "Sirena", "DO_01", 0);

            _dataConcentratorService.AddTag(analogInput);
            _dataConcentratorService.AddTag(digitalInput);
            _dataConcentratorService.AddTag(analogOutput);
            _dataConcentratorService.AddTag(digitalOutput);

            _dataConcentratorService.AddAlarm(new Alarm(80, AlarmTriggerType.AboveLimit, "Temperatura je iznad dozvoljene granice.", analogInput.Id));
            _dataConcentratorService.AddAlarm(new Alarm(20, AlarmTriggerType.BelowLimit, "Temperatura je ispod dozvoljene granice.", analogInput.Id));

            _plcSimulator.EnsureAddress(analogInput.IOAddress, 78);
            _plcSimulator.EnsureAddress(digitalInput.IOAddress, 1);
            _plcSimulator.Write(analogInput.IOAddress, 78);
            _plcSimulator.Write(digitalInput.IOAddress, 1);

            SyncCollections();
            StatusMessage = "Učitani demo tagovi i alarmi.";
            AppendActivity(StatusMessage);
        }

        private void OpenAddWindow()
        {
            EnsureCanWrite();
            try
            {
                var addWindow = new Views.AddWindow(_dataConcentratorService, _validationService);
                addWindow.Owner = System.Windows.Application.Current.MainWindow;
                addWindow.ShowDialog();

                if (addWindow.DialogResultValue)
                {
                    SyncCollections();
                    StatusMessage = "Uspešno dodat novi objekat.";
                    AppendActivity(StatusMessage);
                }
            }
            catch (Exception ex)
            {
                LogError("Greška pri dodavanju", ex);
            }
        }

        private void OpenEditWindow()
        {
            EnsureCanWrite();
            if (SelectedTag == null && SelectedAlarm == null)
            {
                StatusMessage = "Izaberi tag ili definisani alarm za izmenu.";
                return;
            }

            try
            {
                Views.AddWindow addWindow;
                if (SelectedTag != null)
                {
                    addWindow = new Views.AddWindow(_dataConcentratorService, _validationService, SelectedTag, null);
                }
                else if (SelectedAlarm != null)
                {
                    addWindow = new Views.AddWindow(_dataConcentratorService, _validationService, null, SelectedAlarm);
                }
                else
                {
                    StatusMessage = "Izaberi tag ili definisani alarm za izmenu.";
                    return;
                }

                addWindow.Owner = System.Windows.Application.Current.MainWindow;
                addWindow.ShowDialog();

                if (addWindow.DialogResultValue)
                {
                    SyncCollections();
                    StatusMessage = "Uspešno ažuriran objekat.";
                    AppendActivity(StatusMessage);
                }
            }
            catch (Exception ex)
            {
                LogError("Greška pri izmeni", ex);
            }
        }

        private void ShowTagDetails()
        {
            var analogInputTag = SelectedTag as AnalogInputTag;
            if (analogInputTag == null)
            {
                StatusMessage = "Izaberi AI tag da vidite detalje alarma.";
                return;
            }

            var detailsWindow = new Views.TagDetailsWindow(analogInputTag);
            detailsWindow.Owner = System.Windows.Application.Current.MainWindow;
            detailsWindow.ShowDialog();
        }

        private void ScanInputs()
        {
            SimulatePlcInputChanges();
            _dataConcentratorService.ScanInputs();
            SyncActivatedAlarms();
            RefreshAlarmSoundState();
            StatusMessage = "Scan ulaza je uspešno izvršen.";
            AppendActivity(StatusMessage);
        }

        private void AutoScanInputs()
        {
            SimulatePlcInputChanges();
            _dataConcentratorService.ScanInputsIfDue();
            RefreshAlarmSoundState();
        }

        private void SimulatePlcInputChanges()
        {
            foreach (var analogTag in _dataConcentratorService.Tags.OfType<AnalogInputTag>())
            {
                var swing = _random.NextDouble() * 4 - 2;
                var nextValue = Math.Max(analogTag.LowLimit, Math.Min(analogTag.HighLimit, analogTag.CurrentValue + swing));
                _plcSimulator.Write(analogTag.IOAddress, nextValue);
            }

            foreach (var digitalTag in _dataConcentratorService.Tags.OfType<DigitalInputTag>())
            {
                if (_random.NextDouble() < 0.05)
                {
                    _plcSimulator.Write(digitalTag.IOAddress, _random.Next(0, 2));
                }
            }
        }

        private void ToggleSelectedScan()
        {
            EnsureCanWrite();
            var inputTag = SelectedTag as InputTag;
            if (inputTag == null)
            {
                throw new InvalidOperationException("Izaberi ulazni tag da uključiš ili isključiš scan.");
            }

            _dataConcentratorService.ToggleScan(inputTag.TagName, !inputTag.OnOffScan);
            StatusMessage = string.Format("Scan za '{0}' je sada {1}.", inputTag.TagName, inputTag.OnOffScan ? "uključen" : "isključen");
            AppendActivity(StatusMessage);
        }

        private void WriteSelectedOutputValue()
        {
            EnsureCanWrite();
            var outputTag = SelectedTag as OutputTag;
            if (outputTag == null)
            {
                throw new InvalidOperationException("Izaberi izlazni tag za upis vrednosti.");
            }

            var writeWindow = new Views.WriteValueWindow(outputTag)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            writeWindow.ShowDialog();

            if (!writeWindow.DialogResultValue)
            {
                return;
            }

            _dataConcentratorService.WriteOutputValue(outputTag.TagName, writeWindow.ParsedValue);
            StatusMessage = string.Format("Nova izlazna vrednost za '{0}' je {1:F2}.", outputTag.TagName, writeWindow.ParsedValue);
            AppendActivity(StatusMessage);
        }

        private void AcknowledgeSelectedAlarm()
        {
            EnsureCanWrite();

            if (!HasAcknowledgeableSelection())
            {
                throw new InvalidOperationException("Izaberi aktiviran alarm u stanju Active da bi ga acknowledge-ovao.");
            }

            var alarmId = SelectedActivatedAlarm != null
                ? SelectedActivatedAlarm.AlarmId
                : SelectedAlarm.Id;

            _dataConcentratorService.AcknowledgeAlarm(alarmId);
            RefreshAlarmSoundState();
            StatusMessage = string.Format("Alarm #{0} je acknowledge-ovan.", alarmId);
            AppendActivity(StatusMessage);
        }

        private void PreviewAlarmSound()
        {
            _alarmSoundService.Preview();
            StatusMessage = "Pušten test zvuka alarma.";
            AppendActivity(StatusMessage);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.1) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                RefreshAlarmSoundState();
            };
            timer.Start();
        }

        private void LoadAppearanceSettings()
        {
            AlarmVolumePercent = Settings.Default.AlarmVolume * 100d;

            AlarmSoundProfile savedProfile;
            if (!Enum.TryParse(Settings.Default.AlarmSoundProfile, out savedProfile))
            {
                savedProfile = AlarmSoundProfile.ClassicBeep;
            }

            SelectedAlarmSoundOption = AlarmSoundOptions.FirstOrDefault(option => option.Profile == savedProfile)
                ?? AlarmSoundOptions.First();

            ApplicationTheme savedTheme;
            if (!Enum.TryParse(Settings.Default.ApplicationTheme, out savedTheme))
            {
                savedTheme = ThemeService.CurrentTheme;
            }

            SelectedThemeOption = ThemeOptions.FirstOrDefault(option => option.Theme == savedTheme)
                ?? ThemeOptions.First();
        }

        private void RefreshAlarmSoundState()
        {
            var hasUnacknowledgedAlarms = _dataConcentratorService.Alarms.Any(alarm => alarm.State == AlarmState.Active);
            if (hasUnacknowledgedAlarms)
            {
                if (!_isAlarmSoundActive)
                {
                    _alarmSoundService.Start();
                    _isAlarmSoundActive = true;
                }
            }
            else
            {
                _alarmSoundService.Stop();
                _isAlarmSoundActive = false;
            }
        }

        private void OpenTagValueSearchWindow()
        {
            try
            {
                var searchWindow = new Views.TagValueSearchWindow(_dataConcentratorService)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                searchWindow.ShowDialog();

                if (searchWindow.DialogResultValue)
                {
                    StatusMessage = string.Format("AI pretraga je generisala fajl: {0}", searchWindow.GeneratedFilePath);
                    AppendActivity(StatusMessage);
                }
            }
            catch (Exception ex)
            {
                LogError("Greška pri pretrazi AI vrednosti", ex);
            }
        }

        private void GenerateReport()
        {
            var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            var path = _dataConcentratorService.GenerateReport(reportsDirectory);
            StatusMessage = string.Format("Report je generisan: {0}", path);
            AppendActivity(StatusMessage);
        }

        private void RemoveSelectedTag()
        {
            EnsureCanWrite();
            if (SelectedTag == null)
            {
                throw new InvalidOperationException("Izaberi tag za brisanje.");
            }

            var tagName = SelectedTag.TagName;
            if (_dataConcentratorService.RemoveTag(tagName))
            {
                SyncCollections();
                StatusMessage = string.Format("Tag '{0}' je uklonjen.", tagName);
                AppendActivity(StatusMessage);
                SelectedTag = null;
            }
        }

        private void RemoveSelectedAlarm()
        {
            EnsureCanWrite();
            if (SelectedAlarm == null)
            {
                throw new InvalidOperationException("Izaberi alarm za brisanje.");
            }

            var alarmId = SelectedAlarm.Id;
            if (_dataConcentratorService.RemoveAlarm(alarmId))
            {
                SyncCollections();
                StatusMessage = string.Format("Alarm #{0} je uklonjen.", alarmId);
                AppendActivity(StatusMessage);
                SelectedAlarm = null;
            }
        }

        private void SyncCollections()
        {
            var selectedTagId = SelectedTag != null ? SelectedTag.Id : 0;
            var selectedAlarmId = SelectedAlarm != null ? SelectedAlarm.Id : 0;
            var selectedActivatedAlarmId = SelectedActivatedAlarm != null ? SelectedActivatedAlarm.Id : 0;

            ReplaceCollection(Tags, _dataConcentratorService.Tags);
            ReplaceCollection(Alarms, _dataConcentratorService.Alarms);
            ReplaceCollection(ActivatedAlarms, _dataConcentratorService.ActivatedAlarms.OrderByDescending(a => a.ActivationTime));

            if (selectedTagId > 0)
            {
                SelectedTag = Tags.FirstOrDefault(t => t.Id == selectedTagId);
            }

            if (selectedAlarmId > 0)
            {
                SelectedAlarm = Alarms.FirstOrDefault(a => a.Id == selectedAlarmId);
            }

            if (selectedActivatedAlarmId > 0)
            {
                SelectedActivatedAlarm = ActivatedAlarms.FirstOrDefault(a => a.Id == selectedActivatedAlarmId);
            }

            RefreshAlarmSoundState();
            AcknowledgeAlarmCommand.RaiseCanExecuteChanged();
        }

        private void SyncActivatedAlarms()
        {
            var selectedActivatedAlarmId = SelectedActivatedAlarm != null ? SelectedActivatedAlarm.Id : 0;

            ReplaceCollection(ActivatedAlarms, _dataConcentratorService.ActivatedAlarms.OrderByDescending(a => a.ActivationTime));

            if (selectedActivatedAlarmId > 0)
            {
                SelectedActivatedAlarm = ActivatedAlarms.FirstOrDefault(a => a.Id == selectedActivatedAlarmId);
            }

            AcknowledgeAlarmCommand.RaiseCanExecuteChanged();
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void ExecuteSafely(Action action)
        {
            if (_isShutdown)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogError("Greška", ex);
            }
        }

        private void LogError(string prefix, Exception ex)
        {
            StatusMessage = ex.Message;
            AppendActivity(string.Format("{0}: {1}", prefix, ex.Message));
            _logger.Log(string.Format("ERROR | {0}: {1}", prefix, ex.Message));
        }

        private void DataConcentratorService_TagValueChanged(object sender, TagValueChangedEventArgs e)
        {
            AppendActivity(string.Format("Promenjena vrednost taga '{0}' na {1:F2}.", e.Tag.TagName, e.Tag.CurrentValue));
        }

        private void DataConcentratorService_AlarmRaised(object sender, AlarmRaisedEventArgs e)
        {
            if (_isShutdown)
            {
                return;
            }

            _dataConcentratorService.ReloadActivatedAlarmsFromDatabase();
            var activatedAlarmFromDatabase = _dataConcentratorService.GetActivatedAlarmFromDatabase(e.ActivatedAlarm.Id) ?? e.ActivatedAlarm;

            AppendActivity(string.Format(
                "Alarm (iz baze): {0} | Tag: {1} | Value: {2:F2} | Vreme: {3:dd.MM.yyyy HH:mm:ss}",
                activatedAlarmFromDatabase.Message,
                activatedAlarmFromDatabase.TagName,
                activatedAlarmFromDatabase.Value,
                activatedAlarmFromDatabase.ActivationTime));

            SyncActivatedAlarms();
            RefreshAlarmSoundState();

            _logger.Log(string.Format(
                "UI osvežen iz baze za aktivirani alarm #{0} (ActivatedAlarmId={1}).",
                e.Alarm.Id,
                activatedAlarmFromDatabase.Id));
        }

        private void AppendActivity(string message)
        {
            ActivityFeed.Insert(0, string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, message));
        }

        private static Tuple<IPlcSimulator, IDataConcentratorService, ISystemLogger> CreateDefaultServices()
        {
            var plcSimulator = new PlcSimulator();
            var logger = new FileSystemLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system.log"));
            var repository = new Data.ScadaRepository();
            var dataConcentratorService = new DataConcentratorService(plcSimulator, new TagValidationService(), logger, repository);
            return Tuple.Create(plcSimulator as IPlcSimulator, dataConcentratorService as IDataConcentratorService, logger as ISystemLogger);
        }
    }
}
