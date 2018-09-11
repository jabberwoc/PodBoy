using System;
using System.IO;
using System.Net;
using System.Runtime.Caching;
using System.Windows.Media.Imaging;
using PodBoy.Extension;
using Splat;

namespace PodBoy.Cache
{
    public class ImageMemoryCache : IImageCache
    {
        private static IImageCache instance;
        private static readonly object Lock = new object();

        private readonly CacheItemPolicy policy = new CacheItemPolicy
        {
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        public static IImageCache Instance => instance ?? (instance = new ImageMemoryCache());

        private ImageMemoryCache()
        {
            policy.RemovedCallback = CacheItemRemoved;
            //var cacheSettings = new NameValueCollection(3)
            //{
            //    {
            //        "CacheMemoryLimitMegabytes", Convert.ToString(cacheSize)
            //    },
            //    {
            //        "physicalMemoryLimitPercentage", Convert.ToString(49)
            //    },
            //    {
            //        "pollingInterval", Convert.ToString("00:00:10")
            //    }
            //};

            //cache = new MemoryCache("TestCache", cacheSettings);
        }

        private void CacheItemRemoved(CacheEntryRemovedArguments arguments)
        {
            this.Log().Info("Cache item removed: {0}. Reason: {1}", arguments.CacheItem.Key, arguments.RemovedReason);
        }

        public BitmapImage GetImageFromCache(Uri uri, int size)
        {
            // TODO strong key
            var key = uri.AbsolutePath + size;

            var cached = MemoryCache.Default.Get(key) as BitmapImage;

            if (cached != null)
            {
                return cached;
            }

            lock (Lock)
            {
                cached = MemoryCache.Default.Get(key) as BitmapImage;

                if (cached != null)
                {
                    return cached;
                }

                var imageData = DownloadDataForUri(uri);
                var image = CreateImage(imageData, size);
                if (imageData != null)
                {
                    MemoryCache.Default.Set(key, image, policy);
                }
                return image;
            }
        }

        private BitmapImage CreateImage(byte[] data, int size)
        {
            if (data == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(data))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.DecodePixelHeight = size;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private byte[] DownloadDataForUri(Uri uri)
        {
            try
            {
                var request = WebRequest.Create(uri);
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var resultStream = new MemoryStream();
                    stream.CopyTo(resultStream);

                    return resultStream.GetBuffer();
                }
            }
            catch (WebException e)
            {
                this.Log().WarnException("Error downloading episode image with Uri: {0}".FormatString(uri), e);
                return null;
            }
        }
    }
}