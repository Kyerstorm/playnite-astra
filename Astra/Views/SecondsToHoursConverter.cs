using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Formats a seconds count as whole hours (e.g. 5430 -> "1"), for display next to an "h"/"Hours" label.</summary>
    public class SecondsToHoursConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long seconds)
            {
                return Math.Round(seconds / 3600.0, 1);
            }

            if (value is double secondsDouble)
            {
                return Math.Round(secondsDouble / 3600.0, 1);
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
