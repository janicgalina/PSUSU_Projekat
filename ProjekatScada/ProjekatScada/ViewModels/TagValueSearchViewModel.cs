using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ProjekatScada.Infrastructure;
using ProjekatScada.Models;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.ViewModels
{
    public class TagValueSearchViewModel : ObservableObject
    {
        private readonly IDataConcentratorService _dataConcentratorService;
        private AiTagFilterOption _selectedTagOption;
        private string _fromDateTimeText;
        private string _toDateTimeText;
        private string _fromValueText;
        private string _toValueText;
        private string _validationMessage;
        private string _resultMessage;

        public TagValueSearchViewModel(IDataConcentratorService dataConcentratorService)
        {
            _dataConcentratorService = dataConcentratorService;

            TagOptions = new ObservableCollection<AiTagFilterOption>();
            TagOptions.Add(new AiTagFilterOption { TagName = null, DisplayName = "Svi AI tagovi" });

            foreach (var tag in _dataConcentratorService.Tags.OfType<AnalogInputTag>().OrderBy(t => t.TagName))
            {
                TagOptions.Add(new AiTagFilterOption
                {
                    TagName = tag.TagName,
                    DisplayName = tag.TagName
                });
            }

            SelectedTagOption = TagOptions.First();
            GenerateTxtCommand = new RelayCommand(_ => GenerateTxt());
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        public ObservableCollection<AiTagFilterOption> TagOptions { get; private set; }

        public ICommand GenerateTxtCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }

        public bool DialogResult { get; private set; }
        public string GeneratedFilePath { get; private set; }

        public AiTagFilterOption SelectedTagOption
        {
            get { return _selectedTagOption; }
            set { SetProperty(ref _selectedTagOption, value); }
        }

        public string FromDateTimeText
        {
            get { return _fromDateTimeText; }
            set { SetProperty(ref _fromDateTimeText, value); }
        }

        public string ToDateTimeText
        {
            get { return _toDateTimeText; }
            set { SetProperty(ref _toDateTimeText, value); }
        }

        public string FromValueText
        {
            get { return _fromValueText; }
            set { SetProperty(ref _fromValueText, value); }
        }

        public string ToValueText
        {
            get { return _toValueText; }
            set { SetProperty(ref _toValueText, value); }
        }

        public string ValidationMessage
        {
            get { return _validationMessage; }
            set { SetProperty(ref _validationMessage, value); }
        }

        public string ResultMessage
        {
            get { return _resultMessage; }
            set { SetProperty(ref _resultMessage, value); }
        }

        public event EventHandler RequestClose;

        private void GenerateTxt()
        {
            try
            {
                ValidationMessage = string.Empty;
                ResultMessage = string.Empty;

                var filter = BuildFilter();
                var reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                GeneratedFilePath = _dataConcentratorService.GenerateTagValueHistoryReport(filter, reportsDirectory);
                ResultMessage = string.Format("Report je generisan: {0}", GeneratedFilePath);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ValidationMessage = ex.Message;
            }
        }

        private TagValueHistoryFilter BuildFilter()
        {
            var filter = new TagValueHistoryFilter
            {
                TagName = SelectedTagOption != null ? SelectedTagOption.TagName : null
            };

            DateTime parsedDateTime;
            if (!string.IsNullOrWhiteSpace(FromDateTimeText))
            {
                if (!TryParseDateTime(FromDateTimeText, out parsedDateTime))
                {
                    throw new InvalidOperationException("Polje 'Vreme od' nije validno. Koristite format dd.MM.yyyy HH:mm:ss.");
                }

                filter.FromTime = parsedDateTime;
            }

            if (!string.IsNullOrWhiteSpace(ToDateTimeText))
            {
                if (!TryParseDateTime(ToDateTimeText, out parsedDateTime))
                {
                    throw new InvalidOperationException("Polje 'Vreme do' nije validno. Koristite format dd.MM.yyyy HH:mm:ss.");
                }

                filter.ToTime = parsedDateTime;
            }

            if (filter.FromTime.HasValue && filter.ToTime.HasValue && filter.FromTime > filter.ToTime)
            {
                throw new InvalidOperationException("Vreme 'od' ne može biti posle vremena 'do'.");
            }

            double parsedValue;
            if (!string.IsNullOrWhiteSpace(FromValueText))
            {
                if (!TryParseDouble(FromValueText, out parsedValue))
                {
                    throw new InvalidOperationException("Polje 'Vrednost od' mora biti broj.");
                }

                filter.FromValue = parsedValue;
            }

            if (!string.IsNullOrWhiteSpace(ToValueText))
            {
                if (!TryParseDouble(ToValueText, out parsedValue))
                {
                    throw new InvalidOperationException("Polje 'Vrednost do' mora biti broj.");
                }

                filter.ToValue = parsedValue;
            }

            if (filter.FromValue.HasValue && filter.ToValue.HasValue && filter.FromValue > filter.ToValue)
            {
                throw new InvalidOperationException("Vrednost 'od' ne može biti veća od vrednosti 'do'.");
            }

            return filter;
        }

        private static bool TryParseDateTime(string text, out DateTime value)
        {
            var formats = new[]
            {
                "dd.MM.yyyy HH:mm:ss",
                "dd.MM.yyyy HH:mm",
                "dd.MM.yyyy"
            };

            return DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                || DateTime.TryParse(text.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
        }

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }
}
