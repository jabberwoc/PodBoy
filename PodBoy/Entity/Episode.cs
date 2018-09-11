using System;
using System.Reactive.Linq;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Entity
{
    public class Episode : ReactiveObject, IDetailEntity, IProvidesImage, IPlayable
    {
        private const int ImagePixelWidth = 30;
        private DateTime? date;
        private string description;
        private double elapsedSeconds;
        private string imageUrl;
        private bool isActive;
        private bool isPlayed;
        private bool isPlaying;
        private bool isSelected;
        private string title;

        public Episode()
        {
            this.WhenAnyValue(_ => _.ImageUrl)
                .NotNull()
                .Select(url => new Uri(url, UriKind.Absolute))
                .BindTo(ImageProvider, _ => _.ImageUri);
            this.WhenAnyValue(_ => _.IsPlaying).Where(_ => _ && !IsPlayed).Subscribe(_ => IsPlayed = true);
        }

        public Channel Channel { get; set; }

        public int ChannelId { get; set; }

        public DateTime? Date
        {
            get => date;
            set => this.RaiseAndSetIfChanged(ref date, value);
        }

        public string Description
        {
            get => description;
            set => this.RaiseAndSetIfChanged(ref description, value);
        }

        public double ElapsedSeconds
        {
            get => elapsedSeconds;
            set => this.RaiseAndSetIfChanged(ref elapsedSeconds, value);
        }

        public string Guid { get; set; }

        public int Id { get; set; }

        public IImageProvider ImageProvider { get; } = new ImageProvider(ImagePixelWidth);
        public Uri ImageUri => ImageProvider.ImageUri;

        public string ImageUrl
        {
            get => imageUrl;
            set => this.RaiseAndSetIfChanged(ref imageUrl, value);
        }

        public bool IsActive
        {
            get => isActive;
            set => this.RaiseAndSetIfChanged(ref isActive, value);
        }

        public bool IsPlayed
        {
            get => isPlayed;
            set => this.RaiseAndSetIfChanged(ref isPlayed, value);
        }

        public bool IsPlaying
        {
            get => isPlaying;
            set => this.RaiseAndSetIfChanged(ref isPlaying, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => this.RaiseAndSetIfChanged(ref isSelected, value);
        }

        public string Link { get; set; }

        public virtual Media Media { get; set; }

        public string Title
        {
            get => title;
            set => this.RaiseAndSetIfChanged(ref title, value);
        }
    }
}