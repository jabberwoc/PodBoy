using System;
using System.Windows.Data;

namespace PodBoy.Resource.Converter
{
    public class IsGreaterThanZero : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //return int.Parse(value as string) > 0;
            return (int) value > 0;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}