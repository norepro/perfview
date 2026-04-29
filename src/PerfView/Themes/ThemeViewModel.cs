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
        /// Styles overlay for Classic Dark DataGrid colors.
        /// </summary>
        private static global::Avalonia.Styling.Styles s_classicDarkDataGridStyles;

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
            if (s_classicDarkDataGridStyles != null)
            {
                RemoveDataGridStylesFromAllWindows(s_classicDarkDataGridStyles);
                s_classicDarkDataGridStyles = null;
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
                        ApplyClassicDarkOverrides(darkDict);
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

                    // DataGrid styles must be applied at the window level (not app level)
                    // because the DataGrid StyleInclude's ControlTheme takes precedence
                    // over app-level styles. Build the styles once and apply to all windows.
                    s_classicDarkDataGridStyles = BuildClassicDarkDataGridStyles();
                    ApplyDataGridStylesToAllWindows(s_classicDarkDataGridStyles);
                }
                catch (Exception ex)
                {
                    App.AvaloniaLog($"[ClassicDark] ERROR: {ex}");
                }
            }
            else
            {
                // Restore default dark theme colors if switching away from Classic Dark
                var darkDict2 = app.Resources.ThemeDictionaries.ContainsKey(global::Avalonia.Styling.ThemeVariant.Dark)
                    ? app.Resources.ThemeDictionaries[global::Avalonia.Styling.ThemeVariant.Dark]
                        as global::Avalonia.Controls.ResourceDictionary
                    : null;
                if (darkDict2 != null)
                {
                    RestoreDefaultDarkOverrides(darkDict2);
                }
                app.RequestedThemeVariant = theme switch
                {
                    Theme.Light => global::Avalonia.Styling.ThemeVariant.Light,
                    Theme.Dark => global::Avalonia.Styling.ThemeVariant.Dark,
                    _ => global::Avalonia.Styling.ThemeVariant.Default,  // follows system
                };
            }
        }

        private static global::Avalonia.Media.SolidColorBrush MakeBrush(string color)
            => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse(color));

        /// <summary>Set Classic Dark color overrides on the app's Dark ThemeDictionary.</summary>
        private static void ApplyClassicDarkOverrides(global::Avalonia.Controls.ResourceDictionary dict)
        {
            // Hyperlinks
            dict["HyperlinkButtonForegroundColor"] = MakeBrush("#FFEBEBEB");
            dict["HyperlinkButtonForegroundPointerOver"] = MakeBrush("#FF6CB3FF");

            // DataGrid
            dict["DataGridColumnHeaderBackgroundBrush"] = MakeBrush("#FF343434");
            dict["DataGridColumnHeaderForegroundBrush"] = MakeBrush("#FFEBEBEB");
            dict["DataGridColumnHeaderHoveredBackgroundBrush"] = MakeBrush("#FF3F3F3F");
            dict["DataGridColumnHeaderPressedBackgroundBrush"] = MakeBrush("#FF323232");
            dict["DataGridRowBackgroundBrush"] = MakeBrush("#FF2D2D2D");
            dict["DataGridRowHoveredBackgroundColor"] = MakeBrush("#FF3F3F3F");
            dict["DataGridRowAlternateBackground"] = MakeBrush("#FF424124");
            dict["DataGridCellBackgroundBrush"] = MakeBrush("#00000000");
        }

        /// <summary>Restore default Fluent Dark colors on the app's Dark ThemeDictionary.</summary>
        private static void RestoreDefaultDarkOverrides(global::Avalonia.Controls.ResourceDictionary dict)
        {
            var brush = new System.Func<string, global::Avalonia.Media.SolidColorBrush>(
                c => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse(c)));

            // Hyperlinks — restore default dark blue
            dict["HyperlinkButtonForegroundColor"] = MakeBrush("#FF6CB3FF");
            dict["HyperlinkButtonForegroundPointerOver"] = MakeBrush("#FF99CCFF");

            // DataGrid — remove overrides (set transparent/default)
            dict.Remove("DataGridColumnHeaderBackgroundBrush");
            dict.Remove("DataGridColumnHeaderForegroundBrush");
            dict.Remove("DataGridColumnHeaderHoveredBackgroundBrush");
            dict.Remove("DataGridColumnHeaderPressedBackgroundBrush");
            dict.Remove("DataGridRowBackgroundBrush");
            dict.Remove("DataGridRowHoveredBackgroundColor");
            dict.Remove("DataGridCellBackgroundBrush");
            dict["DataGridRowAlternateBackground"] = MakeBrush("#FF2A2A2A");
        }

        /// <summary>Build the Classic Dark DataGrid style overrides (applied per-window).</summary>
        internal static global::Avalonia.Styling.Styles BuildClassicDarkDataGridStyles()
        {
            var styles = new global::Avalonia.Styling.Styles();

            // DataGridColumnHeader: gray background with white text
            var headerStyle = new global::Avalonia.Styling.Style(x =>
                global::Avalonia.Styling.Selectors.OfType(x, typeof(global::Avalonia.Controls.DataGridColumnHeader)));
            headerStyle.Setters.Add(new global::Avalonia.Styling.Setter(
                global::Avalonia.Controls.DataGridColumnHeader.BackgroundProperty, MakeBrush("#FF343434")));
            headerStyle.Setters.Add(new global::Avalonia.Styling.Setter(
                global::Avalonia.Controls.DataGridColumnHeader.ForegroundProperty, MakeBrush("#FFEBEBEB")));
            styles.Add(headerStyle);

            // DataGridRow: dark gray background (odd rows)
            var rowStyle = new global::Avalonia.Styling.Style(x =>
                global::Avalonia.Styling.Selectors.OfType(x, typeof(global::Avalonia.Controls.DataGridRow)));
            rowStyle.Setters.Add(new global::Avalonia.Styling.Setter(
                global::Avalonia.Controls.DataGridRow.BackgroundProperty, MakeBrush("#FF2D2D2D")));
            styles.Add(rowStyle);

            // DataGridRow alternating: gold/olive for even rows
            var altRowStyle = new global::Avalonia.Styling.Style(x =>
                global::Avalonia.Styling.Selectors.NthChild(
                    global::Avalonia.Styling.Selectors.OfType(x, typeof(global::Avalonia.Controls.DataGridRow)),
                    2, 0));
            altRowStyle.Setters.Add(new global::Avalonia.Styling.Setter(
                global::Avalonia.Controls.DataGridRow.BackgroundProperty, MakeBrush("#FF424124")));
            styles.Add(altRowStyle);

            return styles;
        }

        /// <summary>Build resource overrides needed alongside DataGrid styles (per-window).</summary>
        internal static global::Avalonia.Controls.ResourceDictionary BuildClassicDarkDataGridResources()
        {
            // Make the template's BackgroundRectangle transparent so row Background shows through
            var resources = new global::Avalonia.Controls.ResourceDictionary();
            var darkDict = new global::Avalonia.Controls.ResourceDictionary();
            darkDict["DataGridRowBackgroundBrush"] = MakeBrush("#00000000");
            resources.ThemeDictionaries[global::Avalonia.Styling.ThemeVariant.Dark] = darkDict;
            return resources;
        }

        /// <summary>Apply DataGrid styles to all open windows.</summary>
        internal static void ApplyDataGridStylesToAllWindows(global::Avalonia.Styling.Styles template)
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime is
                global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var window in desktop.Windows)
                {
                    window.Styles.Add(BuildClassicDarkDataGridStyles());
                    window.Resources.MergedDictionaries.Add(BuildClassicDarkDataGridResources());
                }
            }
        }

        /// <summary>Remove DataGrid styles from all open windows.</summary>
        internal static void RemoveDataGridStylesFromAllWindows(global::Avalonia.Styling.Styles ignored)
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime is
                global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var window in desktop.Windows)
                {
                    // Remove DataGrid styles
                    for (int i = window.Styles.Count - 1; i >= 0; i--)
                    {
                        if (window.Styles[i] is global::Avalonia.Styling.Styles s && s.Count > 0 &&
                            s[0] is global::Avalonia.Styling.Style style &&
                            style.Selector?.ToString() == "DataGridColumnHeader")
                        {
                            window.Styles.RemoveAt(i);
                        }
                    }
                    // Remove DataGrid resource overrides
                    for (int i = window.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
                    {
                        if (window.Resources.MergedDictionaries[i] is global::Avalonia.Controls.ResourceDictionary rd &&
                            rd.ThemeDictionaries.Count > 0)
                        {
                            window.Resources.MergedDictionaries.RemoveAt(i);
                        }
                    }
                }
            }
        }

        /// <summary>Get the current Classic Dark DataGrid styles (for new windows).</summary>
        internal static global::Avalonia.Styling.Styles ClassicDarkDataGridStyles => s_classicDarkDataGridStyles;
#endif
    }
}
