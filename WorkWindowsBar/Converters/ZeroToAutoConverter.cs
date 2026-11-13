using System;
using System.Globalization;
using System.Windows.Data;

namespace RetroBar.Converters
{
    /// <summary>
    /// Maps a size of 0 (or less) to double.NaN so WPF sizes the element automatically.
    /// </summary>
    public class ZeroToAutoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double d && d > 0 ? d : double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double d && !double.IsNaN(d) ? d : 0d;
        }
    }
}
