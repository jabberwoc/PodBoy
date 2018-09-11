using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PodBoy
{
    public interface IImageProvider
    {
        Uri ImageUri { get; set; }
        int DecodePixelWidth { get; }

        bool IsDefault { get; set; }

        ImageSource Image { get; set; }

        BitmapImage CreateImage();

        void ResetImage();
    }
}