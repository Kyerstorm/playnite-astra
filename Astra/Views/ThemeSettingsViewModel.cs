using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Astra.Models;
using Astra.Services;

namespace Astra.Views
{
    /// <summary>Astra's own theme/accent picker page (not Playnite's separate Extension Settings
    /// dialog - see CLAUDE.md's "Settings page / UI themes" section for the full scope decision).
    /// Deliberately NOT IYearScoped, like Backlog - theme choice isn't year-scoped.</summary>
    public class ThemeSettingsViewModel : ViewModelBase
    {
        private readonly Astra plugin;
        private readonly AstraSettings settings;

        public List<ThemeOption> ThemeOptions { get; }
        public List<AccentOption> AccentOptions { get; }

        public ICommand SelectThemeCommand { get; }
        public ICommand SelectAccentCommand { get; }
        public ICommand ResetToDefaultCommand { get; }

        public ThemeSettingsViewModel(Astra plugin, AstraSettings settings)
        {
            this.plugin = plugin;
            this.settings = settings;

            ThemeOptions = AstraThemeCatalog.Themes
                .Select(t => new ThemeOption(t.Name, t.Label) { IsSelected = t.Name == settings.ThemeName })
                .ToList();
            AccentOptions = AstraThemeCatalog.Accents
                .Select(a => new AccentOption(a.Name, a.Label, a.Hex) { IsSelected = a.Name == settings.AccentColor })
                .ToList();

            SelectThemeCommand = new RelayCommand(p => SelectTheme((ThemeOption)p));
            SelectAccentCommand = new RelayCommand(p => SelectAccent((AccentOption)p));
            ResetToDefaultCommand = new RelayCommand(_ => ResetToDefault());
        }

        private void SelectTheme(ThemeOption chosen)
        {
            foreach (var option in ThemeOptions)
            {
                option.IsSelected = ReferenceEquals(option, chosen);
            }

            settings.ThemeName = chosen.Name;
            // Theme choice must feel permanent immediately (unlike Year/GridView, which only
            // persist when Playnite's own Extension Settings dialog is closed via EndEdit) - a
            // user who never opens that dialog still expects their theme to survive a restart.
            plugin.SavePluginSettings(settings);
        }

        private void SelectAccent(AccentOption chosen)
        {
            foreach (var option in AccentOptions)
            {
                option.IsSelected = ReferenceEquals(option, chosen);
            }

            settings.AccentColor = chosen.Name;
            plugin.SavePluginSettings(settings);
        }

        private void ResetToDefault()
        {
            SelectTheme(ThemeOptions.First(o => o.Name == AstraThemeCatalog.DefaultTheme));
            SelectAccent(AccentOptions.First(o => o.Name == AstraThemeCatalog.DefaultAccent));
        }
    }
}
