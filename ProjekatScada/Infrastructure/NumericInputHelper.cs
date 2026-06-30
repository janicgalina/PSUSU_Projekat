using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjekatScada.Infrastructure
{
    public static class NumericInputHelper
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(NumericInputHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty AllowDecimalProperty =
            DependencyProperty.RegisterAttached(
                "AllowDecimal",
                typeof(bool),
                typeof(NumericInputHelper),
                new PropertyMetadata(true));

        public static readonly DependencyProperty AllowNegativeProperty =
            DependencyProperty.RegisterAttached(
                "AllowNegative",
                typeof(bool),
                typeof(NumericInputHelper),
                new PropertyMetadata(true));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static bool GetAllowDecimal(DependencyObject obj)
        {
            return (bool)obj.GetValue(AllowDecimalProperty);
        }

        public static void SetAllowDecimal(DependencyObject obj, bool value)
        {
            obj.SetValue(AllowDecimalProperty, value);
        }

        public static bool GetAllowNegative(DependencyObject obj)
        {
            return (bool)obj.GetValue(AllowNegativeProperty);
        }

        public static void SetAllowNegative(DependencyObject obj, bool value)
        {
            obj.SetValue(AllowNegativeProperty, value);
        }

        public static bool IsValidNumericText(string text, bool allowDecimal, bool allowNegative)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            if (!allowNegative && text.Contains("-"))
            {
                return false;
            }

            var pattern = allowDecimal
                ? @"^-?\d*([.,]\d*)?$"
                : @"^-?\d*$";

            return Regex.IsMatch(text, pattern);
        }

        public static bool TryParseDouble(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return double.TryParse(
                text.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        public static bool TryParseInt(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = d as TextBox;
            if (textBox == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += OnPreviewTextInput;
                DataObject.AddPastingHandler(textBox, OnPaste);
            }
            else
            {
                textBox.PreviewTextInput -= OnPreviewTextInput;
                DataObject.RemovePastingHandler(textBox, OnPaste);
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var proposed = GetProposedText(textBox, e.Text);
            e.Handled = !IsValidNumericText(
                proposed,
                GetAllowDecimal(textBox),
                GetAllowNegative(textBox));
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            var textBox = (TextBox)sender;
            var pasteText = e.DataObject.GetData(typeof(string)) as string ?? string.Empty;
            var proposed = GetProposedText(textBox, pasteText);
            if (!IsValidNumericText(
                proposed,
                GetAllowDecimal(textBox),
                GetAllowNegative(textBox)))
            {
                e.CancelCommand();
            }
        }

        private static string GetProposedText(TextBox textBox, string input)
        {
            var text = textBox.Text ?? string.Empty;
            var selectionStart = textBox.SelectionStart;
            var selectionLength = textBox.SelectionLength;
            return text.Remove(selectionStart, selectionLength).Insert(selectionStart, input);
        }
    }
}
