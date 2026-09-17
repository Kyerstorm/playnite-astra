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
