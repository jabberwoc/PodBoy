using System;
using System.Windows;
using System.Windows.Data;

namespace PodBoy.Resource.Converter
{
    public class BooleanOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            foreach (object value in values)
            {
                if (value == DependencyProperty.UnsetValue)
                {
                    return false;
                }

                if ((bool) value)
                {
                    return true;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value,
            Type[] targetTypes,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}