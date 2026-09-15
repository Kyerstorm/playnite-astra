using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Astra.Views
{
    /// <summary>Converts a "#RRGGBB" string to a frozen SolidColorBrush, falling back to a neutral gray.</summary>
    public class ColorHexToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Fallback = Freeze(new SolidColorBrush(Color.FromRgb(0x4a, 0x4a, 0x4a)));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrEmpty(hex))
            {
                return Fallback;
            }

            try
            {
                return Freeze(new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)));
            }
            catch
            {
                return Fallback;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
