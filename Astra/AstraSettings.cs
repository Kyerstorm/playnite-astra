using System;
using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace Astra
{
    public class AstraSettings : ViewModelBase
    {
        private string displayName = string.Empty;
        private int lastSelectedYear = DateTime.Now.Year;
        private int mostPlayedDisplayCount = 25;
        private bool mostPlayedGridView;
        private string themeName = "Playnite";
        private string accentColor = "Teal";
        private bool themePromptShown;

        public string DisplayName
        {
            get => displayName;
            set => SetValue(ref displayName, value);
        }

        public int LastSelectedYear
        {
            get => lastSelectedYear;
            set => SetValue(ref lastSelectedYear, value);
        }

        /// <summary>How many rows Most Played shows, or int.MaxValue for "All".</summary>
        public int MostPlayedDisplayCount
        {
            get => mostPlayedDisplayCount;
            set => SetValue(ref mostPlayedDisplayCount, value);
        }

        public bool MostPlayedGridView
        {
            get => mostPlayedGridView;
            set => SetValue(ref mostPlayedGridView, value);
        }

        /// <summary>"Playnite" is the sentinel meaning "no Astra theme dictionary merged" (today's
        /// behavior, unchanged). Any other value maps to a resource in Astra/Themes/ via
        /// AstraThemeCatalog.ResourcePathFor.</summary>
        public string ThemeName
        {
            get => themeName;
            set => SetValue(ref themeName, value);
        }

        /// <summary>One of AstraThemeCatalog.Accents' names. Only visually affects custom themes
        /// (Dark/Light/Oled) - "Follow Playnite theme" never overrides GlyphBrush.</summary>
        public string AccentColor
        {
            get => accentColor;
            set => SetValue(ref accentColor, value);
        }

        /// <summary>Whether the one-time "pick a theme" first-run landing (Astra shell opens
        /// directly to the Settings page instead of Home) has already happened.</summary>
        public bool ThemePromptShown
        {
            get => themePromptShown;
            set => SetValue(ref themePromptShown, value);
        }
    }

    public class AstraSettingsViewModel : ViewModelBase, ISettings
    {
        private readonly Astra plugin;
        private AstraSettings editingClone;

        private AstraSettings settings;
        public AstraSettings Settings
        {
            get => settings;
            set => SetValue(ref settings, value);
        }

        public AstraSettingsViewModel(Astra plugin)
        {
            this.plugin = plugin;
            var savedSettings = plugin.LoadPluginSettings<AstraSettings>();
            Settings = savedSettings ?? new AstraSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
