using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>
    /// Turns an element's own ActualWidth into a Height that keeps a fixed aspect
    /// ratio (ConverterParameter = height/width, e.g. "1.5" for a 2:3 cover) - used
    /// so a cover cell can stretch to fill a UniformGrid column's width while
    /// staying a real cover shape, without hardcoding a fixed pixel size.
    /// </summary>
    public class AspectRatioHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double width) || width <= 0)
            {
                return 0.0;
            }

            var ratio = parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : 1.5;
            return width * ratio;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
