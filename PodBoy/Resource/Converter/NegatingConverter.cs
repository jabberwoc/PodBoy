using System;
using System.Windows;
using System.Windows.Data;

namespace PodBoy.Resource.Converter
{
    public class NegatingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                return -((double) value);
            }
            if (value is bool)
            {
                return !(bool) value;
            }
            if (value is Visibility)
            {
                return (Visibility) value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                return +(double) value;
            }
            return value;
        }
    }
}