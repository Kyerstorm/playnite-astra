using System;
using System.Globalization;
using System.Windows.Data;
using Astra.Models;

namespace Astra.Views
{
    /// <summary>Formats a GameRecapEntry's Trend + RankDelta (values[0], values[1]) into the Most Played row's trend badge text.</summary>
    public class TrendTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is RankTrend trend) || !(values[1] is int delta))
            {
                return string.Empty;
            }

            switch (trend)
            {
                case RankTrend.New:
                    return "NEW";
                case RankTrend.Up:
                    return $"▲{delta}";
                case RankTrend.Down:
                    return $"▼{-delta}";
                default:
                    return "–";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
