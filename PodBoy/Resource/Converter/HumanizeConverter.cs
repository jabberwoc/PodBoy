using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;

namespace PodBoy.Resource.Converter
{
    public class HumanizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as DateTime?)?.Humanize();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}