using System;
using System.Globalization;
using System.Windows.Data;

namespace PodBoy.Resource.Converter
{
    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            if (stringValue == null)
            {
                return 0;
            }

            return double.Parse(stringValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}