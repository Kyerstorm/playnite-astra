using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Turns a 0-1 "BarFraction" into a star-sized GridLength so a horizontal bar can be drawn
    /// with two ColumnDefinitions (filled + remainder) instead of a fixed-pixel-width hack that wouldn't
    /// resize with the column. ConverterParameter "remainder" returns (1 - fraction) instead of fraction,
    /// so both columns of the same row can bind to the same source value.</summary>
    public class BarFractionToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fraction = value is double d ? d : 0;
            fraction = Math.Max(0, Math.Min(1, fraction));

            if (string.Equals(parameter as string, "remainder", StringComparison.OrdinalIgnoreCase))
            {
                fraction = 1 - fraction;
            }

            // A zero-width star column is valid but leaves nothing to render - floor it slightly so an
            // all-zero (empty range) bar list still shows visible (empty) tracks rather than collapsing.
            return new GridLength(Math.Max(0.001, fraction), GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
