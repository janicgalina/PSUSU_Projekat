using System;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ProjekatScada.Infrastructure;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IDataConcentratorService _dataConcentratorService;
        private readonly IPlcSimulator _plcSimulator;
        private readonly Random _random = new Random();
        private TagBase _selectedTag;
        private Alarm _selectedAlarm;
        private ActivatedAlarm _selectedActivatedAlarm;
        private string _statusMessage;

        public MainViewModel()
            : this(CreateDefaultServices())
        {
        }

        private MainViewModel(Tuple<IPlcSimulator, IDataConcentratorService> services)
            : this(services.Item1, services.Item2)
        {
        }

        public MainViewModel(IPlcSimulator plcSimulator, IDataConcentratorService dataConcentratorService)
        {
            _plcSimulator = plcSimulator;
            _dataConcentratorService = dataConcentratorService;

            Tags = new ObservableCollection<TagBase>();
            Alarms = new ObservableCollection<Alarm>();
            ActivatedAlarms = new ObservableCollection<ActivatedAlarm>();
            ActivityFeed = new ObservableCollection<string>();

            LoadDemoDataCommand = new RelayCommand(_ => LoadDemoData());
            ScanCommand = new RelayCommand(_ => ExecuteSafely(ScanInputs));
            ToggleScanCommand = new RelayCommand(_ => ExecuteSafely(ToggleSelectedScan));
            WriteOutputCommand = new RelayCommand(_ => ExecuteSafely(WriteSelectedOutputValue));
            AcknowledgeAlarmCommand = new RelayCommand(_ => ExecuteSafely(AcknowledgeSelectedAlarm));
            GenerateReportCommand = new RelayCommand(_ => ExecuteSafely(GenerateReport));
            RemoveSelectedTagCommand = new RelayCommand(_ => ExecuteSafely(RemoveSelectedTag));

            _dataConcentratorService.TagValueChanged += DataConcentratorService_TagValueChanged;
            _dataConcentratorService.AlarmRaised += DataConcentratorService_AlarmRaised;

            StatusMessage = "Spremno za inicijalizaciju SCADA sistema.";
        }

        public ObservableCollection<TagBase> Tags { get; private set; }
        public ObservableCollection<Alarm> Alarms { get; private set; }
        public ObservableCollection<ActivatedAlarm> ActivatedAlarms { get; private set; }
        public ObservableCollection<string> ActivityFeed { get; private set; }

        public ICommand LoadDemoDataCommand { get; private set; }
        public ICommand ScanCommand { get; private set; }
        public ICommand ToggleScanCommand { get; private set; }
        public ICommand WriteOutputCommand { get; private set; }
        public ICommand AcknowledgeAlarmCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand RemoveSelectedTagCommand { get; private set; }

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

        private void LoadDemoData()
        {
            if (Tags.Any())
            {
                StatusMessage = "Demo podaci su već učitani.";
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
            StatusMessage = "Učitani demo tagovi i alarmi za dalji razvoj.";
            AppendActivity(StatusMessage);
        }

        private void ScanInputs()
        {
            foreach (var analogTag in _dataConcentratorService.Tags.OfType<AnalogInputTag>())
            {
                var swing = _random.NextDouble() * 12 - 6;
                var nextValue = Math.Max(analogTag.LowLimit, Math.Min(analogTag.HighLimit, analogTag.CurrentValue + swing));
                _plcSimulator.Write(analogTag.IOAddress, nextValue);
            }

            foreach (var digitalTag in _dataConcentratorService.Tags.OfType<DigitalInputTag>())
            {
                _plcSimulator.Write(digitalTag.IOAddress, _random.Next(0, 2));
            }

            _dataConcentratorService.ScanInputs();
            SyncCollections();
            StatusMessage = "Scan ulaza je uspešno izvršen.";
            AppendActivity(StatusMessage);
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

            double value;
            var analogOutput = outputTag as AnalogOutputTag;
            if (analogOutput != null)
            {
                value = Math.Round(analogOutput.LowLimit + (_random.NextDouble() * (analogOutput.HighLimit - analogOutput.LowLimit)), 2);
            }
            else
            {
                value = outputTag.CurrentValue >= 0.5 ? 0 : 1;
            }

            _dataConcentratorService.WriteOutputValue(outputTag.TagName, value);
            SyncCollections();
            StatusMessage = string.Format("Nova izlazna vrednost za '{0}' je {1:F2}.", outputTag.TagName, value);
            AppendActivity(StatusMessage);
        }

        private void AcknowledgeSelectedAlarm()
        {
            if (SelectedAlarm == null)
            {
                throw new InvalidOperationException("Izaberi alarm koji želiš da acknowledge-uješ.");
            }

            _dataConcentratorService.AcknowledgeAlarm(SelectedAlarm.Id);
            SyncCollections();
            StatusMessage = string.Format("Alarm #{0} je acknowledge-ovan.", SelectedAlarm.Id);
            AppendActivity(StatusMessage);
        }

        private void GenerateReport()
        {
            var path = _dataConcentratorService.GenerateReport(AppDomain.CurrentDomain.BaseDirectory);
            StatusMessage = string.Format("Report je generisan: {0}", path);
            AppendActivity(StatusMessage);
        }

        private void RemoveSelectedTag()
        {
            if (SelectedTag == null)
            {
                throw new InvalidOperationException("Izaberi tag za brisanje.");
            }

            if (_dataConcentratorService.RemoveTag(SelectedTag.TagName))
            {
                SyncCollections();
                StatusMessage = string.Format("Tag '{0}' je uklonjen.", SelectedTag.TagName);
                AppendActivity(StatusMessage);
                SelectedTag = null;
            }
        }

        private void SyncCollections()
        {
            ReplaceCollection(Tags, _dataConcentratorService.Tags);
            ReplaceCollection(Alarms, _dataConcentratorService.Alarms);
            ReplaceCollection(ActivatedAlarms, _dataConcentratorService.ActivatedAlarms.OrderByDescending(a => a.ActivationTime));
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
                StatusMessage = ex.Message;
                AppendActivity(string.Format("Greška: {0}", ex.Message));
            }
        }

        private void DataConcentratorService_TagValueChanged(object sender, TagValueChangedEventArgs e)
        {
            AppendActivity(string.Format("Promenjena vrednost taga '{0}' na {1:F2}.", e.Tag.TagName, e.Tag.CurrentValue));
        }

        private void DataConcentratorService_AlarmRaised(object sender, AlarmRaisedEventArgs e)
        {
            AppendActivity(string.Format("Alarm: {0} | Tag: {1} | Value: {2:F2}", e.Alarm.Message, e.ActivatedAlarm.TagName, e.ActivatedAlarm.Value));
            SyncCollections();
        }

        private void AppendActivity(string message)
        {
            ActivityFeed.Insert(0, string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, message));
        }

        private static Tuple<IPlcSimulator, IDataConcentratorService> CreateDefaultServices()
        {
            var plcSimulator = new PlcSimulator();
            var logger = new FileSystemLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system.log"));
            var dataConcentratorService = new DataConcentratorService(plcSimulator, new TagValidationService(), logger);
            return Tuple.Create(plcSimulator as IPlcSimulator, dataConcentratorService as IDataConcentratorService);
        }
    }
}
