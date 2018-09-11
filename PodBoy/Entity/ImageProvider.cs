using System;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PodBoy.Cache;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Entity
{
    public class ImageProvider : ReactiveObject, IImageProvider
    {
        private ImageSource image;
        private ImageSource defaultImage;
        private bool isDefault;

        public Uri ImageUri { get; set; }
        public int DecodePixelWidth { get; }
        public BitmapCreateOptions BitmapCreateOptions { get; }

        public ImageProvider(Uri imageUri,
            int decodePixelWidth,
            BitmapCreateOptions bitmapCreateOptions = BitmapCreateOptions.IgnoreColorProfile)
        {
            ImageUri = imageUri;
            DecodePixelWidth = decodePixelWidth;
            BitmapCreateOptions = bitmapCreateOptions;
        }

        public ImageProvider(int decodePixelWidth,
            BitmapCreateOptions bitmapCreateOptions = BitmapCreateOptions.IgnoreColorProfile)
            : this(null, decodePixelWidth, bitmapCreateOptions) {}

        public virtual ImageSource DefaultImage => defaultImage ?? (defaultImage = AppResources.Instance.DefaultImage);

        public bool IsDefault
        {
            get => isDefault;
            set => this.RaiseAndSetIfChanged(ref isDefault, value);
        }

        public ImageSource Image
        {
            get
            {
                if (image != null)
                {
                    return image;
                }

                if (ImageUri == null)
                {
                    IsDefault = true;
                    return image = DefaultImage;
                }

                try
                {
                    IsDefault = true;

                    Observable.Start(CreateImage, RxApp.TaskpoolScheduler)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ =>
                        {
                            Image = _;
                            IsDefault = false;
                        });

                    return DefaultImage;
                }
                catch (Exception)
                {
                    IsDefault = true;
                    return image = DefaultImage;
                }
            }
            set => this.RaiseAndSetIfChanged(ref image, value);
        }

        public virtual BitmapImage CreateImage()
        {
            return ImageMemoryCache.Instance.GetImageFromCache(ImageUri, DecodePixelWidth);
        }

        public void ResetImage()
        {
            image = null;
            this.RaisePropertyChanged(() => Image);
        }
    }
}