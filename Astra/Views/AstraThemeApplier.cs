using System;
using System.Windows;
using System.Windows.Media;
using Astra.Services;

namespace Astra.Views
{
    /// <summary>WPF-only half of theme application: merges/removes the active theme
    /// ResourceDictionary on a given FrameworkElement's own Resources (never
    /// Application.Current.Resources - keeps this an Astra-only skin, never touching Playnite's
    /// real theme or other plugins). Not unit-testable (needs a live FrameworkElement) - same
    /// documented gap as ShareCardRenderer/TrendChartControl; verified manually only.</summary>
    internal static class AstraThemeApplier
    {
        // Tracks the dictionary we last added, so re-applying always removes the old one first
        // instead of accumulating merged dictionaries across repeated theme switches in one
        // session. Safe as a static field: exactly one AstraShellView instance is alive at a time
        // (Playnite recreates it fresh each time the sidebar item's Opened() factory runs).
        private static ResourceDictionary activeThemeDictionary;

        public static void Apply(FrameworkElement root, Astra plugin, string themeName, string accentColor)
        {
            if (activeThemeDictionary != null)
            {
                root.Resources.MergedDictionaries.Remove(activeThemeDictionary);
                activeThemeDictionary = null;
            }

            var path = AstraThemeCatalog.ResourcePathFor(themeName);
            if (path == null)
            {
                return; // "Follow Playnite theme": zero override, matches today's behavior exactly
            }

            try
            {
                // A plain relative Uri (e.g. "Themes/Dark.xaml") only resolves when WPF's XAML
                // loader supplies an ambient base URI, as it does for a Source="..." attribute
                // written directly in XAML (e.g. SharedStyles.xaml's own merges). Building a
                // ResourceDictionary imperatively from C# code has no such ambient base URI, so
                // the same relative Uri silently fails to resolve here - it needs an explicit
                // pack URI naming the assembly instead. Confirmed via a real install: every theme
                // failed with this exact symptom (ShowErrorMessage firing for all three) until
                // this fix.
                var packUri = new Uri($"pack://application:,,,/Astra;component/{path}", UriKind.Absolute);
                var dict = new ResourceDictionary { Source = packUri };
                // Overlays the accent swatch onto GlyphBrush - the resource key every existing
                // Astra view already uses as its de-facto accent (StatValue foreground, BarTrack
                // fill, the nav's ActiveAccentBar). Only applied for custom themes: "Follow
                // Playnite theme" returns above before reaching this line.
                dict["GlyphBrush"] = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(AstraThemeCatalog.HexFor(accentColor)));

                root.Resources.MergedDictionaries.Add(dict);
                activeThemeDictionary = dict;
            }
            catch (Exception)
            {
                plugin.Api.Dialogs.ShowErrorMessage(
                    $"Astra couldn't load the \"{themeName}\" theme and will use \"Follow Playnite theme\" instead.",
                    "Astra Theme");
            }
        }
    }
}
