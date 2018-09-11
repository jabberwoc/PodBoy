using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PodBoy.Extension;
using PodBoy.Notification;

namespace PodBoy.Resource.Converter
{
    public class NotificationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = (NotificationType) value;
            return Application.Current.FindResource("NotificationColor{0}".FormatString(value)); // as Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}