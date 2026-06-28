using System;
using System.Globalization;
using System.Windows;
using ProjekatScada.Models;

namespace ProjekatScada.Views
{
    public partial class WriteValueWindow : Window
    {
        public WriteValueWindow(OutputTag outputTag)
        {
            InitializeComponent();
            OutputTag = outputTag;

            var analogOutput = outputTag as AnalogOutputTag;
            if (analogOutput != null)
            {
                PromptText = string.Format("Unesite vrednost za '{0}' ({1} - {2} {3}):",
                    outputTag.TagName,
                    analogOutput.LowLimit,
                    analogOutput.HighLimit,
                    analogOutput.Units);
                ValueText = outputTag.CurrentValue.ToString("F2", CultureInfo.InvariantCulture);
            }
            else
            {
                PromptText = string.Format("Unesite vrednost za '{0}' (0 ili 1):", outputTag.TagName);
                ValueText = outputTag.CurrentValue >= 0.5 ? "1" : "0";
            }

            DataContext = this;
        }

        public OutputTag OutputTag { get; private set; }
        public string PromptText { get; private set; }
        public string ValueText { get; set; }
        public string ValidationMessage { get; set; }
        public double ParsedValue { get; private set; }
        public bool DialogResultValue { get; private set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            double value;
            if (!double.TryParse(ValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !double.TryParse(ValueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                ValidationMessage = "Vrednost mora biti broj.";
                DataContext = null;
                DataContext = this;
                return;
            }

            var analogOutput = OutputTag as AnalogOutputTag;
            if (analogOutput != null && (value < analogOutput.LowLimit || value > analogOutput.HighLimit))
            {
                ValidationMessage = "Vrednost mora biti unutar zadatih granica.";
                DataContext = null;
                DataContext = this;
                return;
            }

            var digitalOutput = OutputTag as DigitalOutputTag;
            if (digitalOutput != null && value != 0d && value != 1d)
            {
                ValidationMessage = "DO vrednost mora biti 0 ili 1.";
                DataContext = null;
                DataContext = this;
                return;
            }

            ParsedValue = value;
            DialogResultValue = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResultValue = false;
            Close();
        }
    }
}
