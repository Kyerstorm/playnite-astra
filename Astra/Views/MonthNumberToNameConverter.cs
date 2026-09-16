using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Formats a 1-12 month number (GamingRhythmStats.BusiestMonth) as its full invariant
    /// name, or an em dash when null (no sessions in range).</summary>
    public class MonthNumberToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int month && month >= 1 && month <= 12)
            {
                return new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);
            }

            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
