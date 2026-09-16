using System;
using System.Globalization;
using System.Windows.Data;

namespace Astra.Views
{
    /// <summary>Floors a 0-1 intensity value so a cell with a little activity still reads as visibly
    /// different from a fully empty one - a raw Intensity of e.g. 0.02 would otherwise be
    /// indistinguishable from 0 at normal opacity/contrast.</summary>
    public class IntensityToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var intensity = value is double d ? d : 0;
            return intensity <= 0 ? 0.0 : Math.Max(0.18, intensity);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
