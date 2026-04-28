using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Security;
using System.Windows;
using System.Windows.Input;
using Utilities;

namespace PerfView
{
    public enum Theme
    {
        Light,
        Dark,
        System
    }

    public class ThemeViewModel : INotifyPropertyChanged
    {
        private ConfigData _userConfigData;
        public Theme CurrentTheme { get; private set; }

        public bool IsLightTheme
        {
            get => CurrentTheme == Theme.Light;
            set => SetTheme(Theme.Light);
        }

        public bool IsDarkTheme
        {
            get => CurrentTheme == Theme.Dark;
            set => SetTheme(Theme.Dark);
        }

        public bool IsSystemTheme
        {
            get => CurrentTheme == Theme.System;
            set => SetTheme(Theme.System);
        }

#if !AVALONIA
        public class SetThemeCommand : RoutedCommand
        {
            public SetThemeCommand(Theme theme)
            {
                Theme = theme;
            }

            public Theme Theme { get; }
        }

        public static SetThemeCommand SetLightThemeCommand = new SetThemeCommand(Theme.Light);

        public static SetThemeCommand SetDarkThemeCommand = new SetThemeCommand(Theme.Dark);

        public static SetThemeCommand SetSystemThemeCommand = new SetThemeCommand(Theme.System);
#endif

        public event PropertyChangedEventHandler PropertyChanged;

        public ThemeViewModel(ConfigData userConfigData)
        {
            _userConfigData = userConfigData;
        }

        public static void InitTheme(Theme theme)
        {
            if (theme == Theme.System)
            {
                try
                {
                    // Check current system theme via registry
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value is int i)
                        {
                            theme = i != 0 ? Theme.Light : Theme.Dark;
                        }
                        else
                        {
                            // registry key not found or is not int, use Light theme by default
                            theme = Theme.Light;
                        }
                    }
                }
                catch (SecurityException)
                {
                    // We don't have access to registry
                    theme = Theme.Light;
                }
            }

#if !AVALONIA
            if (theme == Theme.Light)
                ApplyResources("Themes/LightTheme.xaml");
            else// if (newTheme == Theme.Dark)
                ApplyResources("Themes/DarkTheme.xaml");

            void ApplyResources(string src)
            {
                var dict = new ResourceDictionary() { Source = new Uri(src, UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries[0] = dict;
            }
#else
            ApplyAvaloniaTheme(theme);
#endif
        }

        public void SetTheme(Theme newTheme)
        {
            if (newTheme == CurrentTheme)
                return;

            Theme oldTheme = CurrentTheme;

            _userConfigData["Theme"] = newTheme.ToString();
            CurrentTheme = newTheme;

#if AVALONIA
            ApplyAvaloniaTheme(newTheme);
#endif

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Is{newTheme}Theme"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Is{oldTheme}Theme"));
        }

#if AVALONIA
        /// <summary>
        /// Apply the theme variant to the running Avalonia application at runtime.
        /// </summary>
        private static void ApplyAvaloniaTheme(Theme theme)
        {
            if (global::Avalonia.Application.Current == null)
                return;

            global::Avalonia.Application.Current.RequestedThemeVariant = theme switch
            {
                Theme.Light => global::Avalonia.Styling.ThemeVariant.Light,
                Theme.Dark => global::Avalonia.Styling.ThemeVariant.Dark,
                _ => global::Avalonia.Styling.ThemeVariant.Default,  // follows system
            };
        }
#endif
    }
}