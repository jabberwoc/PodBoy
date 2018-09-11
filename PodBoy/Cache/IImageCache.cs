using System;
using System.Windows.Media.Imaging;
using Splat;

namespace PodBoy.Cache
{
    public interface IImageCache : IEnableLogger
    {
        BitmapImage GetImageFromCache(Uri imageUri, int decodePixelWidth);
    }
}