using System.Windows;

namespace ShowPlayer.App.Services
{
    public static class ThemeManager
    {
        private static readonly ResourceDictionary ThemeDictionary = new();

        public static void Initialize(bool isDark)
        {
            if (!Application.Current.Resources.MergedDictionaries.Contains(ThemeDictionary))
                Application.Current.Resources.MergedDictionaries.Add(ThemeDictionary);
            Apply(isDark);
        }

        public static void Apply(bool isDark)
        {
            var baseUri = new Uri("pack://application:,,,/ShowPlayer.App;component/Themes/");
            ThemeDictionary.Source = new Uri(baseUri, isDark ? "DarkTheme.xaml" : "LightTheme.xaml");
        }
    }
}
