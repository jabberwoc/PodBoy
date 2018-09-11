using System;

namespace PodBoy.Cache
{
    public class ImageKey
    {
        public ImageKey(Uri uri, int pixelWidth)
        {
            Uri = uri;
            PixelWidth = pixelWidth;
        }

        public int PixelWidth { get; private set; }

        public Uri Uri { get; private set; }
    }
}