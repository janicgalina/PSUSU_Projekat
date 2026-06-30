using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
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
        private double _alarmVolumePercent = 70;
        private AlarmSoundOption _selectedAlarmSoundOption;
        private ThemeOption _selectedThemeOption;
        private bool _isAlarmSoundActive;

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

            LoadDemoDataCommand = new RelayCommand(_ => LoadDemoData());
            OpenAddWindowCommand = new RelayCommand(_ => OpenAddWindow());
            EditSelectedTagCommand = new RelayCommand(_ => OpenEditWindow());
            ShowTagDetailsCommand = new RelayCommand(_ => ShowTagDetails());
            ScanCommand = new RelayCommand(_ => ExecuteSafely(ScanInputs));
            ToggleScanCommand = new RelayCommand(_ => ExecuteSafely(ToggleSelectedScan));
            WriteOutputCommand = new RelayCommand(_ => ExecuteSafely(WriteSelectedOutputValue));
            AcknowledgeAlarmCommand = new RelayCommand(_ => ExecuteSafely(AcknowledgeSelectedAlarm));
            GenerateReportCommand = new RelayCommand(_ => ExecuteSafely(GenerateReport));
            ExportConfigurationCommand = new RelayCommand(_ => ExecuteSafely(ExportConfiguration));
            ImportConfigurationCommand = new RelayCommand(_ => ExecuteSafely(ImportConfiguration));
            OpenTagValueSearchCommand = new RelayCommand(_ => OpenTagValueSearchWindow());
            RemoveSelectedTagCommand = new RelayCommand(_ => ExecuteSafely(RemoveSelectedTag));
            RemoveSelectedAlarmCommand = new RelayCommand(_ => ExecuteSafely(RemoveSelectedAlarm));
            PreviewAlarmSoundCommand = new RelayCommand(_ => ExecuteSafely(PreviewAlarmSound));

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

        public ICommand LoadDemoDataCommand { get; private set; }
        public ICommand OpenAddWindowCommand { get; private set; }
        public ICommand EditSelectedTagCommand { get; private set; }
        public ICommand ShowTagDetailsCommand { get; private set; }
        public ICommand ScanCommand { get; private set; }
        public ICommand ToggleScanCommand { get; private set; }
        public ICommand WriteOutputCommand { get; private set; }
        public ICommand AcknowledgeAlarmCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ExportConfigurationCommand { get; private set; }
        public ICommand ImportConfigurationCommand { get; private set; }
        public ICommand OpenTagValueSearchCommand { get; private set; }
        public ICommand RemoveSelectedTagCommand { get; private set; }
        public ICommand RemoveSelectedAlarmCommand { get; private set; }
        public ICommand PreviewAlarmSoundCommand { get; private set; }

        public TagBase SelectedTag
        {
            get { return _selectedTag; }
            set { SetProperty(ref _selectedTag, value); }
        }

        public Alarm SelectedAlarm
        {
            get { return _selectedAlarm; }
            set { SetProperty(ref _selectedAlarm, value); }
        }

        public ActivatedAlarm SelectedActivatedAlarm
        {
            get { return _selectedActivatedAlarm; }
            set { SetProperty(ref _selectedActivatedAlarm, value); }
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

        public void InitializeAfterLogin(string username)
        {
            CurrentUser = username;
            _logger.Log(string.Format("Login korisnika '{0}'.", username));

            try
            {
                _dataConcentratorService.LoadFromRepository();
                SyncCollections();
                StatusMessage = string.Format("Dobrodošli, {0}. Podaci su učitani iz baze.", username);
            }
            catch (Exception ex)
            {
                _logger.Log(string.Format("ERROR | Greška pri učitavanju baze: {0}", ex.Message));
                StatusMessage = "Baza je prazna ili nedostupna. Možete učitati demo podatke.";
            }

            AppendActivity(StatusMessage);
        }

        private void LoadDemoData()
        {
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
            if (SelectedTag == null && SelectedAlarm == null)
            {
                StatusMessage = "Izaberi tag ili definisani alarm za izmenu.";
                return;
            }

            try
            {
                var addWindow = SelectedAlarm != null
                    ? new Views.AddWindow(_dataConcentratorService, _validationService, null, SelectedAlarm)
                    : new Views.AddWindow(_dataConcentratorService, _validationService, SelectedTag, null);

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
            SyncCollections();
            StatusMessage = "Scan ulaza je uspešno izvršen.";
            AppendActivity(StatusMessage);
        }

        private void AutoScanInputs()
        {
            SimulatePlcInputChanges();
            _dataConcentratorService.ScanInputsIfDue();
            SyncCollections();
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
            SyncCollections();
            StatusMessage = string.Format("Nova izlazna vrednost za '{0}' je {1:F2}.", outputTag.TagName, writeWindow.ParsedValue);
            AppendActivity(StatusMessage);
        }

        private void AcknowledgeSelectedAlarm()
        {
            var alarmId = SelectedActivatedAlarm != null
                ? SelectedActivatedAlarm.AlarmId
                : SelectedAlarm != null ? SelectedAlarm.Id : 0;

            if (alarmId == 0)
            {
                throw new InvalidOperationException("Izaberi alarm koji želiš da acknowledge-uješ.");
            }

            _dataConcentratorService.AcknowledgeAlarm(alarmId);
            SyncCollections();
            RefreshAlarmSoundState();
            StatusMessage = string.Format("Alarm #{0} je acknowledge-ovan.", alarmId);
            AppendActivity(StatusMessage);
        }

        private void PreviewAlarmSound()
        {
            _alarmSoundService.Preview();
            StatusMessage = "Pušten test zvuka alarma.";
            AppendActivity(StatusMessage);
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
            else if (_isAlarmSoundActive)
            {
                _alarmSoundService.Stop();
                _isAlarmSoundActive = false;
                _logger.Log("Zvuk alarma je isključen.");
            }
        }

        private void GenerateReport()
        {
            var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            var path = _dataConcentratorService.GenerateReport(reportsDirectory);
            StatusMessage = string.Format("Report je generisan: {0}", path);
            AppendActivity(StatusMessage);
        }

        private void ExportConfiguration()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON fajlovi (*.json)|*.json",
                FileName = "scada_config.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _dataConcentratorService.ExportConfiguration(dialog.FileName);
            StatusMessage = string.Format("Konfiguracija je exportovana u {0}.", dialog.FileName);
            AppendActivity(StatusMessage);
        }

        private void ImportConfiguration()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON fajlovi (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var replaceExisting = Tags.Any() &&
                System.Windows.MessageBox.Show(
                    "Da li želite da zamenite postojeću konfiguraciju?",
                    "Import konfiguracije",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;

            _dataConcentratorService.ImportConfiguration(dialog.FileName, replaceExisting);
            SyncCollections();
            StatusMessage = string.Format("Konfiguracija je importovana iz {0}.", dialog.FileName);
            AppendActivity(StatusMessage);
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

        private void RemoveSelectedTag()
        {
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
            ReplaceCollection(Tags, _dataConcentratorService.Tags);
            ReplaceCollection(Alarms, _dataConcentratorService.Alarms);
            ReplaceCollection(ActivatedAlarms, _dataConcentratorService.ActivatedAlarms.OrderByDescending(a => a.ActivationTime));
            RefreshAlarmSoundState();
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
            AppendActivity(string.Format("Alarm: {0} | Tag: {1} | Value: {2:F2}", e.Alarm.Message, e.ActivatedAlarm.TagName, e.ActivatedAlarm.Value));
            SyncCollections();
            if (!_isAlarmSoundActive)
            {
                _alarmSoundService.Start();
                _isAlarmSoundActive = true;
            }

            _logger.Log(string.Format("Pušten zvuk alarma za alarm #{0}.", e.Alarm.Id));
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
