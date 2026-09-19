using System.Collections.Generic;

namespace Astra.Services
{
    /// <summary>Pure, WPF-free mapping between Astra's theme/accent setting names and the
    /// resources they resolve to (a Themes/*.xaml relative path, or an accent hex color). Kept
    /// free of System.Windows.* types so it's unit-testable without a WPF Dispatcher/STA thread -
    /// the WPF-dependent half (actually loading/merging a ResourceDictionary) lives in
    /// Astra.Views.AstraThemeApplier instead.</summary>
    public static class AstraThemeCatalog
    {
        public static readonly IReadOnlyList<(string Name, string Label)> Themes = new[]
        {
            ("Playnite", "Follow Playnite"),
            ("Dark", "Dark"),
            ("Light", "Light"),
            ("Oled", "OLED"),
        };

        public static readonly IReadOnlyList<(string Name, string Label, string Hex)> Accents = new[]
        {
            ("Teal", "Teal", "#3EC6E0"),
            ("Purple", "Purple", "#9B7EDE"),
            ("Orange", "Orange", "#F0A356"),
            ("Green", "Green", "#5FCF8E"),
            ("Pink", "Pink", "#E87FB0"),
            ("Blue", "Blue", "#5A9DE8"),
        };

        public const string DefaultTheme = "Playnite";
        public const string DefaultAccent = "Teal";

        /// <summary>Relative resource path for a theme name, or null for "Playnite" (and any
        /// unrecognized value) meaning "apply zero override dictionary" - today's default
        /// behavior, unchanged.</summary>
        public static string ResourcePathFor(string themeName)
        {
            switch (themeName)
            {
                case "Dark": return "Themes/Dark.xaml";
                case "Light": return "Themes/Light.xaml";
                case "Oled": return "Themes/Oled.xaml";
                default: return null;
            }
        }

        /// <summary>Hex color for an accent name. Falls back to the first accent (Teal) for an
        /// unrecognized name (e.g. a corrupted settings file, or a value from a future version)
        /// rather than throwing.</summary>
        public static string HexFor(string accentName)
        {
            foreach (var accent in Accents)
            {
                if (accent.Name == accentName)
                {
                    return accent.Hex;
                }
            }

            return Accents[0].Hex;
        }
    }
}
