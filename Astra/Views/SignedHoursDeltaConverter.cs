using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Formats a seconds delta as "+18.4h vs previous period" / "-3.2h vs previous period" -
    /// purely numerical (see TrendsViewModel), never colored red/green since more/less playtime isn't
    /// good or bad.</summary>
    public class SignedHoursDeltaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is long deltaSeconds))
            {
                return string.Empty;
            }

            var hours = Math.Round(deltaSeconds / 3600.0, 1);
            var sign = hours >= 0 ? "+" : "";
            return $"{sign}{hours.ToString("0.#", CultureInfo.InvariantCulture)}h vs previous period";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
