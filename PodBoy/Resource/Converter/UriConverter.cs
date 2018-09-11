using System;
using System.Globalization;
using System.Windows.Data;

namespace PodBoy.Resource.Converter
{
    public class UriConverter : IValueConverter
    {
        private readonly Uri defaultUri = ResourceHelper.LoadResourceUri("Resource/Graphic/default_image.png");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return defaultUri;
            }

            var uriString = value as string;
            if (uriString == null)
            {
                throw new ArgumentException("value to convert must be a string");
            }
            return new Uri(uriString);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}