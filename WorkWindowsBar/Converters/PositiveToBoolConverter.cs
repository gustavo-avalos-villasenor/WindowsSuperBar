using System;
using System.Globalization;
using System.Windows.Data;

namespace RetroBar.Converters
{
    /// <summary>True when the bound number is greater than zero.</summary>
    public class PositiveToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double d && d > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
