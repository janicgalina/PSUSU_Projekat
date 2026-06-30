using System;
using System.Linq;
using System.Windows;
using ProjekatScada.Models.Enums;
using ProjekatScada.Properties;

namespace ProjekatScada.Services
{
    public static class ThemeService
    {
        public static ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Light;

        public static void Initialize()
        {
            var savedTheme = Settings.Default.ApplicationTheme;
            ApplicationTheme theme;
            if (!Enum.TryParse(savedTheme, out theme))
            {
                theme = ApplicationTheme.Light;
            }

            ApplyTheme(theme, false);
        }

        public static void ApplyTheme(ApplicationTheme theme, bool saveSettings = true)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var dictionaries = app.Resources.MergedDictionaries;
            var existingTheme = dictionaries
                .FirstOrDefault(dictionary =>
                    dictionary.Source != null &&
                    dictionary.Source.OriginalString.Contains("Themes/") &&
                    dictionary.Source.OriginalString.EndsWith("Theme.xaml"));

            if (existingTheme != null)
            {
                dictionaries.Remove(existingTheme);
            }

            var themeUri = theme == ApplicationTheme.Dark
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            dictionaries.Insert(0, new ResourceDictionary { Source = themeUri });

            CurrentTheme = theme;

            if (saveSettings)
            {
                Settings.Default.ApplicationTheme = theme.ToString();
                Settings.Default.Save();
            }
        }
    }
}
