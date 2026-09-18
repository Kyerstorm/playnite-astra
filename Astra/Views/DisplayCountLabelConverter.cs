using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Formats a Most Played display-count option, turning the int.MaxValue "no cap" sentinel into "All".</summary>
    public class DisplayCountLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int count))
            {
                return string.Empty;
            }

            return count == int.MaxValue ? "All" : $"Top {count}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
