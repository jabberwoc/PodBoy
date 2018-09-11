using System;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceModel.Syndication;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Entity
{
    public class Channel : ReactiveObject, IDetailEntity, IOrderedEntity, IProvidesImage
    {
        private int unplayedCount;
        private DateTime? lastUpdated;
        private int orderNumber;
        private string imageUrl;
        private const int ImagePixelWidth = 40;

        public Channel(string link)
            : this()
        {
            Link = link;
        }

        protected Channel()
        {
            this.WhenAnyValue(_ => _.ImageUrl)
                .NotNull()
                .Select(url => new Uri(url, UriKind.Absolute))
                .BindTo(ImageProvider, _ => _.ImageUri);
        }

        public string Description { get; set; }
        public Uri ImageUri => ImageProvider.ImageUri;

        public ReactiveList<Episode> Episodes { get; } = new ReactiveList<Episode>();

        public int Id { get; set; }

        public string ImageUrl
        {
            get => imageUrl;
            set => this.RaiseAndSetIfChanged(ref imageUrl, value);
        }

        public string Link { get; protected set; }

        public string Title { get; set; }

        public int UnplayedCount
        {
            get => unplayedCount;
            set => this.RaiseAndSetIfChanged(ref unplayedCount, value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Channel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 37;
                hash = hash * 23 + GetType().GetHashCode();
                hash = hash * 23 + Link?.GetHashCode() ?? 0;
                //hash = hash * 23 + Id.GetHashCode();
                return hash;
            }
        }

        public bool HasEpisode(SyndicationItem feedItem)
        {
            if (feedItem.Id == null)
            {
                return Episodes.Any(e => Equals(e.Date, feedItem.PublishDate.LocalDateTime));
            }

            return Episodes.Any(e => Equals(e.Guid, feedItem.Id));
        }

        protected bool Equals(Channel other)
        {
            return Equals(Link, other.Link);
        }

        public DateTime? LastUpdated
        {
            get => lastUpdated;
            set => this.RaiseAndSetIfChanged(ref lastUpdated, value);
        }

        public int OrderNumber
        {
            get => orderNumber;
            set => this.RaiseAndSetIfChanged(ref orderNumber, value);
        }

        public IImageProvider ImageProvider { get; } = new ImageProvider(ImagePixelWidth);
    }
}