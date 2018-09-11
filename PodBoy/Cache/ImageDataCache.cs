using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;
using MoreLinq;
using PodBoy.Extension;
using Splat;

namespace PodBoy.Cache
{
    public class ImageDataCache : IImageCache
    {
        private const int DefaultCapacity = 100;
        private static readonly object Lock = new object();
        private static IImageCache instance;
        private readonly Dictionary<Uri, byte[]> cache;
        private readonly int capacity;
        private readonly Dictionary<Uri, int> touchTimes;

        public ImageDataCache()
            : this(DefaultCapacity) {}

        public static IImageCache Instance => instance ?? (instance = Locator.Current.GetService<IImageCache>());

        public ImageDataCache(int capacity, IEqualityComparer<Uri> comparer = null)
        {
            this.capacity = capacity;
            var keyComparer = comparer ?? new UriComparer();
            cache = new Dictionary<Uri, byte[]>(this.capacity, keyComparer);
            touchTimes = new Dictionary<Uri, int>(this.capacity, keyComparer);
        }

        public int Count => cache.Count;

        public BitmapImage GetDataFromCache(Uri imageUri)
        {
            throw new NotImplementedException();
            //if (imageUri == null)
            //{
            //    throw new ArgumentNullException(nameof(imageUri));
            //}

            //byte[] imageData;

            //var fastCached = cache.TryGetValue(imageUri, out imageData);

            //if (fastCached)
            //{
            //    touchTimes[imageUri]++;
            //    return imageData;
            //}

            //lock (Lock)
            //{
            //    var cached = cache.TryGetValue(imageUri, out imageData);

            //    if (cached)
            //    {
            //        touchTimes[imageUri]++;
            //        return imageData;
            //    }

            //    imageData = DownloadDataForUri(imageUri);
            //    if (imageData != null)
            //    {
            //        AddToCache(imageUri, imageData);
            //    }
            //}
            //return imageData;
        }

        private void AddToCache(Uri key, byte[] imageData)
        {
            if (cache.Count == capacity)
            {
                RemoveLeastFrequentlyUsed();
            }
            cache.Add(key, imageData);
            touchTimes.Add(key, 0);
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

        public BitmapImage GetImageFromCache(Uri imageUri, int decodePixelWidth)
        {
            throw new NotImplementedException();
        }

        private void RemoveLeastFrequentlyUsed()
        {
            var minFrequencyKey = touchTimes.MinBy(_ => _.Value).Key;

            cache.Remove(minFrequencyKey);
            touchTimes.Remove(minFrequencyKey);
        }

        private class UriComparer : IEqualityComparer<Uri>
        {
            public bool Equals(Uri uri1, Uri uri2)
            {
                return Equals(uri1.AbsolutePath, uri2.AbsolutePath);
            }

            public int GetHashCode(Uri uri)
            {
                return uri.AbsolutePath.GetHashCode() * 17;
            }
        }
    }
}