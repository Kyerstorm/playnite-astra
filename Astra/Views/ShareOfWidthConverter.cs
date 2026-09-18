using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>
    /// Multiplies a 0-1 share (values[0]) by a container's own ActualWidth (values[1]) to get
    /// a pixel Width for the Most Played row progress bar - avoids needing the row's total
    /// width baked into a converter parameter, since row width varies with the window.
    /// </summary>
    public class ShareOfWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is double share) || !(values[1] is double totalWidth) || totalWidth <= 0)
            {
                return 0.0;
            }

            return Math.Max(0.0, Math.Min(share, 1.0)) * totalWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
