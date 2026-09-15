using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Formats a seconds count (double, e.g. GameRecapEntry.AvgSessionSeconds) as whole minutes.</summary>
    public class SecondsToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double seconds)
            {
                return Math.Round(seconds / 60.0, 1);
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
