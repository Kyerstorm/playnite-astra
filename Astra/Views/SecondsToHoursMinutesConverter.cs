using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Formats a seconds count as "1h 22m" / "8m" / "0m" - used for Gaming Rhythm and Session
    /// Activity figures, which the spec shows in hours+minutes rather than the decimal-hours format
    /// SecondsToHoursConverter uses for the top summary cards.</summary>
    public class SecondsToHoursMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long totalSeconds;
            switch (value)
            {
                case long l:
                    totalSeconds = l;
                    break;
                case double d:
                    totalSeconds = (long)Math.Round(d);
                    break;
                case int i:
                    totalSeconds = i;
                    break;
                default:
                    return "0m";
            }

            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;

            if (hours <= 0)
            {
                return $"{minutes}m";
            }

            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
