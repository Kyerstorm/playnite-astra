using System.Linq;
using System.Text.RegularExpressions;

namespace Astra.Services
{
    /// <summary>
    /// Deterministic "colored initials" fallback for a game with no resolvable
    /// cover art, ported from the Astra UI mockup's colorFor/initialsFor JS so
    /// the same game always gets the same tile across app restarts. Pure
    /// string/math logic, no WPF types, so it's usable from both XAML
    /// converters and unit tests.
    /// </summary>
    public static class CoverPlaceholder
    {
        private static readonly Regex NonAlphanumericOrSpace = new Regex("[^a-zA-Z0-9 ]");

        public static string ColorHexFor(string name)
        {
            uint hash = 0;
            foreach (var c in name ?? string.Empty)
            {
                hash = unchecked(hash * 31 + c);
            }

            var hue = hash % 360;
            var (r, g, b) = HslToRgb(hue, 0.42, 0.34);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public static string InitialsFor(string name)
        {
            var words = NonAlphanumericOrSpace.Replace(name ?? string.Empty, "")
                .Split(' ')
                .Where(w => w.Length > 0)
                .ToList();

            if (words.Count == 0)
            {
                return "?";
            }

            if (words.Count == 1)
            {
                return words[0].Substring(0, System.Math.Min(2, words[0].Length)).ToUpperInvariant();
            }

            return (words[0][0].ToString() + words[1][0]).ToUpperInvariant();
        }

        private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
        {
            h /= 360.0;
            double r, g, b;

            if (s <= 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 1.0 / 3);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3);
            }

            return ((byte)System.Math.Round(r * 255), (byte)System.Math.Round(g * 255), (byte)System.Math.Round(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }
    }
}
