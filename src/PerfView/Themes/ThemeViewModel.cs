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
        ClassicDark,
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

        public bool IsClassicDarkTheme
        {
            get => CurrentTheme == Theme.ClassicDark;
            set => SetTheme(Theme.ClassicDark);
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

        public static SetThemeCommand SetClassicDarkThemeCommand = new SetThemeCommand(Theme.ClassicDark);

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
        /// Overlay resource dictionary applied when the Classic Dark theme is active.
        /// </summary>
        private static global::Avalonia.Controls.ResourceDictionary s_classicDarkResources;

        /// <summary>
        /// Style overlay for Classic Dark link underlines.
        /// </summary>
        private static global::Avalonia.Styling.Style s_classicDarkLinkStyle;

        /// <summary>
        /// Apply the theme variant to the running Avalonia application at runtime.
        /// For ClassicDark, we use the Fluent Dark base and merge WPF-era color overrides.
        /// </summary>
        private static void ApplyAvaloniaTheme(Theme theme)
        {
            var app = global::Avalonia.Application.Current;
            if (app == null)
                return;

            // Remove classic dark overlay if present
            if (s_classicDarkResources != null)
            {
                app.Resources.MergedDictionaries.Remove(s_classicDarkResources);
                s_classicDarkResources = null;
            }
            if (s_classicDarkLinkStyle != null)
            {
                app.Styles.Remove(s_classicDarkLinkStyle);
                s_classicDarkLinkStyle = null;
            }

            if (theme == Theme.ClassicDark)
            {
                app.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
                try
                {
                    s_classicDarkResources = (global::Avalonia.Controls.ResourceDictionary)
                        global::Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(
                            new Uri("avares://PerfView.Avalonia/Assets/ClassicDarkTheme.axaml"));
                    app.Resources.MergedDictionaries.Add(s_classicDarkResources);

                    // Override app-level themed resources directly — merged ThemeDictionaries
                    // can't override the app's own ThemeDictionaries at the same level.
                    var darkDict = app.Resources.ThemeDictionaries[global::Avalonia.Styling.ThemeVariant.Dark]
                        as global::Avalonia.Controls.ResourceDictionary;
                    if (darkDict != null)
                    {
                        darkDict["HyperlinkButtonForegroundColor"] =
                            new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#FFEBEBEB"));
                        darkDict["HyperlinkButtonForegroundPointerOver"] =
                            new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#FF6CB3FF"));
                    }

                    // Add style for underlined links (WPF dark theme uses white underlined links)
                    // Use Inline.TextDecorationsProperty as attached property on Button — 
                    // the ContentPresenter inherits it and applies to rendered text.
                    s_classicDarkLinkStyle = new global::Avalonia.Styling.Style(x =>
                        global::Avalonia.Styling.Selectors.Class(
                            global::Avalonia.Styling.Selectors.OfType<global::Avalonia.Controls.Button>(x),
                            "link"));
                    s_classicDarkLinkStyle.Setters.Add(new global::Avalonia.Styling.Setter(
                        global::Avalonia.Controls.Documents.Inline.TextDecorationsProperty,
                        global::Avalonia.Media.TextDecorations.Underline));
                    app.Styles.Add(s_classicDarkLinkStyle);
                    App.AvaloniaLog($"[ClassicDark] Added link underline style. Selector: {s_classicDarkLinkStyle.Selector}");
                }
                catch (Exception ex)
                {
                    App.AvaloniaLog($"[ClassicDark] ERROR: {ex}");
                }
            }
            else
            {
                // Restore default link color if switching away from Classic Dark
                var darkDict2 = app.Resources.ThemeDictionaries.ContainsKey(global::Avalonia.Styling.ThemeVariant.Dark)
                    ? app.Resources.ThemeDictionaries[global::Avalonia.Styling.ThemeVariant.Dark]
                        as global::Avalonia.Controls.ResourceDictionary
                    : null;
                if (darkDict2 != null && darkDict2.ContainsKey("HyperlinkButtonForegroundColor"))
                {
                    darkDict2["HyperlinkButtonForegroundColor"] =
                        new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#FF6CB3FF"));
                    darkDict2["HyperlinkButtonForegroundPointerOver"] =
                        new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#FF99CCFF"));
                }
                app.RequestedThemeVariant = theme switch
                {
                    Theme.Light => global::Avalonia.Styling.ThemeVariant.Light,
                    Theme.Dark => global::Avalonia.Styling.ThemeVariant.Dark,
                    _ => global::Avalonia.Styling.ThemeVariant.Default,  // follows system
                };
            }
        }
#endif
    }
}