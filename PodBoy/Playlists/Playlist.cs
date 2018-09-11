using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using MoreLinq;
using PodBoy.Entity;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Playlists
{
    public class Playlist : ReactiveObject, IPlaylist, IOrderedEntity
    {
        private PlaylistItem current;
        private bool isSelected;
        private string name;
        private PlaylistItem next;
        private int orderNumber;
        private PlaylistItem previous;

        protected IReactiveList<PlaylistItem> items = new ReactiveList<PlaylistItem>
        {
            ChangeTrackingEnabled = true
        };

        private int itemsCount;

        public Playlist(IEnumerable<Episode> collection)
            : this()
        {
            items.AddRange(collection.Select(_ =>
            {
                var item = new PlaylistItem(_)
                {
                    Playlist = this
                };
                return item;
            }));
        }

        public Playlist(Episode source)
            : this()
        {
            var item = new PlaylistItem(source)
            {
                Playlist = this
            };
            items.Add(item);
        }

        public Playlist()
        {
            this.WhenAnyValue(_ => _.Current).NotNull().Subscribe(_ => OnCurrentChanged());
            this.WhenAnyObservable(_ => _.Items.CountChanged)
                .Throttle(TimeSpan.FromSeconds(.5))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnItemsChanged());

            this.WhenAnyValue(_ => _.Items)
                .NotNull()
                .Where(_ => _.ChangeTrackingEnabled == false)
                .Subscribe(i => i.ChangeTrackingEnabled = true);
        }

        public Playlist(string name)
            : this()
        {
            Name = name;
        }

        public PlaylistItem Current
        {
            get => current;
            set => this.RaiseAndSetIfChanged(ref current, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => this.RaiseAndSetIfChanged(ref isSelected, value);
        }

        public IReactiveList<PlaylistItem> Items
        {
            get => items;
            set => this.RaiseAndSetIfChanged(ref items, value);
        }

        public int ItemsCount
        {
            get => itemsCount;
            set => this.RaiseAndSetIfChanged(ref itemsCount, value);
        }

        public int Id { get; set; }

        public PlaylistType Type => Id == 0 ? PlaylistType.Podcast : PlaylistType.User;

        public void SetCurrent(IPlayable playable)
        {
            if (!(playable is Episode episode))
            {
                throw new InvalidOperationException($"IPlayable parameter should be of type {typeof(Episode)} :>");
            }

            var item = Items.FirstOrDefault(_ => Equals(_.Item, episode));
            if (item != null)
            {
                Current = item;
            }
        }

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public PlaylistItem Next
        {
            get => next;
            private set => this.RaiseAndSetIfChanged(ref next, value);
        }

        public int OrderNumber
        {
            get => orderNumber;
            set => this.RaiseAndSetIfChanged(ref orderNumber, value);
        }

        public PlaylistItem Previous
        {
            get => previous;
            private set => this.RaiseAndSetIfChanged(ref previous, value);
        }

        public int? CurrentId { get; set; }

        public PlaylistItem this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public virtual void Add(Episode element)
        {
            Items.Add(new PlaylistItem(element));
        }

        public void Add(IPlayable item)
        {
            var episode = item as Episode;
            if (episode == null)
            {
                throw new InvalidOperationException($"IPlayable parameter should be of type {typeof(Episode)}");
            }

            Add(episode);
        }

        public virtual void AddItem(PlaylistItem item)
        {
            Items.Add(item);
            item.Playlist = this;
        }

        public virtual void RemoveItem(PlaylistItem item)
        {
            Items.Remove(item);
        }

        public void Add(IEnumerable<IPlayable> newItems)
        {
            Add(newItems.Cast<Episode>());
        }

        public void Add(IEnumerable<Episode> newItems)
        {
            newItems.ForEach(_ => Items.Add(new PlaylistItem(_)));
        }

        public void AddItems(IEnumerable<PlaylistItem> newItems)
        {
            Items.AddRange(newItems);
        }

        public bool AnyItems()
        {
            return Items.Any();
        }

        public void PlayNext()
        {
            Current = Next;
        }

        public void PlayPrevious()
        {
            Current = Previous;
        }

        private void OnCurrentChanged()
        {
            CurrentId = Current.Id;
            SetPreviousNext();
        }

        private void OnItemsChanged()
        {
            SetPreviousNext();
        }

        public void SetActive()
        {
            if (Current == null)
            {
                return;
            }

            SetCurrentActive();
        }

        private void SetCurrentActive()
        {
            if (Current == null)
            {
                return;
            }

            ClearActive();
            Current.IsActive = true;
        }

        public void SetCurrentPlaying(bool isPlaying)
        {
            Current.IsPlaying = isPlaying;
        }

        public void ClearActive()
        {
            if (Current == null)
            {
                return;
            }

            ResetFlags(Items.Where(_ => _.IsActive));
        }

        private void ResetFlags(IEnumerable<PlaylistItem> resetItems)
        {
            foreach (var toReset in resetItems)
            {
                if (toReset == null)
                {
                    return;
                }

                toReset.IsPlaying = false;
                toReset.IsActive = false;
            }
        }

        private void SetPreviousNext()
        {
            if (Current == null)
            {
                Previous = default(PlaylistItem);
                Next = default(PlaylistItem);

                return;
            }

            var currentIndex = Items.IndexOf(current);

            if (currentIndex == -1)
            {
                this.Log().Error($"{Current} not in playlist");
                Current = default(PlaylistItem);
                return;
            }
            var previousIndex = Math.Max(currentIndex - 1, 0);
            var nextIndex = Math.Min(currentIndex + 1, Items.Count - 1);

            Previous = previousIndex != currentIndex ? Items[previousIndex] : default(PlaylistItem);
            Next = nextIndex != currentIndex ? Items[nextIndex] : default(PlaylistItem);
        }

        public override string ToString()
        {
            return $"{Name}[{ItemsCount}]";
        }

        public static IPlaylist Empty()
        {
            return new Playlist();
        }
    }
}