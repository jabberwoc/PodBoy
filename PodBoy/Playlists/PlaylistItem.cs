using System;
using System.Reactive.Linq;
using PodBoy.Entity;
using ReactiveUI;

namespace PodBoy.Playlists
{
    public class PlaylistItem : PlaylistItem<Episode>, IPlaylistItem, IOrderedEntity
    {
        public PlaylistItem() {}

        public PlaylistItem(Episode item)
            : base(item)
        {
            ItemId = Item?.Id ?? 0;
        }

        public PlaylistItem(int itemId)
        {
            ItemId = itemId;
        }

        public Playlist Playlist { get; set; }
        public int Id { get; set; }
        public int PlaylistId { get; protected set; }

        public int ItemId { get; protected set; }
    }

    public class PlaylistItem<T> : ReactiveObject, IPlaylistItem<T> where T : IPlayable
    {
        private bool isActive;
        private bool isPlaying;
        private int orderNumber;

        public PlaylistItem(T item)
            : this()
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            Item = item;
        }

        protected PlaylistItem()
        {
            this.WhenAnyValue(_ => _.IsActive).Where(_ => Item != null).Subscribe(_ => Item.IsActive = _);
            this.WhenAnyValue(_ => _.IsPlaying).Where(_ => Item != null).Subscribe(_ => Item.IsPlaying = _);
        }

        public int OrderNumber
        {
            get => orderNumber;
            set => this.RaiseAndSetIfChanged(ref orderNumber, value);
        }

        public T Item { get; protected set; }

        public bool IsActive
        {
            get => isActive;
            set => this.RaiseAndSetIfChanged(ref isActive, value);
        }

        public bool IsPlaying
        {
            get => isPlaying;
            set => this.RaiseAndSetIfChanged(ref isPlaying, value);
        }

        public bool IsPlayed { get; set; }
    }

    public interface IPlaylistItem<out T> : IHasOrder, IPlayable
    {
        T Item { get; }
    }

    public interface IPlaylistItem : IPlaylistItem<Episode>
    {
        Playlist Playlist { get; set; }
    }
}