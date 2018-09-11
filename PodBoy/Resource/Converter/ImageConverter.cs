using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PodBoy.Resource.Converter
{
    public class ImageConverter : IValueConverter
    {
        private readonly Uri defaultUri = ResourceHelper.LoadResourceUri("Resource/Graphic/reactive_default_30.png");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uri = value as Uri;
            if (uri == null)
            {
                return new BitmapImage(defaultUri);
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.DecodePixelWidth = int.Parse((string) parameter);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = uri;
                image.EndInit();

                while (image.IsDownloading)
                {
                    WpfUtil.DoEvents();
                }
                return image;
            }
            catch
            {
                return new BitmapImage(defaultUri);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}