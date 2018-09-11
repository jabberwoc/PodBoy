using System.Collections.Generic;
using PodBoy.Entity;
using ReactiveUI;

namespace PodBoy.Playlists
{
    public interface IPlaylist<T> : IOrderedEntity
    {
        IPlaylistItem<T> Current { get; set; }
        IPlaylistItem<T> Next { get; }
        IPlaylistItem<T> Previous { get; }

        string Name { get; set; }

        IReactiveList<IPlaylistItem<T>> Items { get; }
        int ItemsCount { get; }

        void Add(T item);

        void AddItem(IPlaylistItem<T> item);
        void Add(IEnumerable<T> items);

        void AddItems(IEnumerable<IPlaylistItem<T>> items);
        bool AnyItems();

        IPlaylistItem<T> this[int index] { get; set; }

        void PlayNext();
        void PlayPrevious();

        bool IsVirtual { get; }
    }

    public interface IPlaylist : IHasOrder
    {
        PlaylistItem Current { get; set; }
        PlaylistItem Next { get; }
        PlaylistItem Previous { get; }

        string Name { get; set; }

        IReactiveList<PlaylistItem> Items { get; }
        int ItemsCount { get; }

        void Add(IPlayable item);

        void AddItem(PlaylistItem item);

        void RemoveItem(PlaylistItem item);

        void Add(IEnumerable<IPlayable> items);

        void AddItems(IEnumerable<PlaylistItem> items);
        bool AnyItems();

        PlaylistItem this[int index] { get; set; }

        void PlayNext();
        void PlayPrevious();

        int Id { get; }

        PlaylistType Type { get; }
        void SetCurrent(IPlayable playable);

        void SetCurrentPlaying(bool isPlaying);
        void ClearActive();

        void SetActive();
    }
}