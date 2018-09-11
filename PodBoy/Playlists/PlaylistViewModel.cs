using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Entity.Store;
using PodBoy.Extension;
using PodBoy.ViewModel;
using Reactive.EventAggregator;
using ReactiveUI;

namespace PodBoy.Playlists
{
    public class PlaylistViewModel : ReactiveViewModel, IRoutableViewModel, IOrderedListManager<Playlist>,
                                     IOrderedListManager<PlaylistItem>, IPlayableViewModel, IDeactivatable
    {
        private readonly ObservableAsPropertyHelper<IDetailEntity> currentDetailEntity;
        private int lastSelectedPlaylistId;
        private bool isPlaylistItemsBusy;
        private bool isPlaylistsBusy;
        private string playlistItemFilterText;
        private ReplaySubject<IOrderByExpression<PlaylistItem>[]> playlistItemOrderBy;
        private IReactiveDerivedList<PlaylistItem> playlistItems;
        private ReactiveList<Playlist> playlists;
        private Playlist selectedPlaylist;
        private PlaylistItem selectedPlaylistItem;
        private IReactiveDerivedList<Playlist> playlistView;
        private readonly IPlaylistStore playlistStore;

        public PlaylistViewModel(IScreen hostScreen,
            IEventAggregator eventAggregator,
            IPlayerModel player,
            IPlaylistStore playlistStore)
            : base(eventAggregator)
        {
            HostScreen = hostScreen;
            Player = player;

            this.playlistStore = playlistStore;

            GetPlaylistsCommand = ReactiveCommand.CreateFromTask(async _ => await GetPlaylists());
            GetPlaylistsCommand.IsExecuting.Subscribe(_ => IsPlaylistsBusy = _);

            InitPlaylistItemsCommand = ReactiveCommand.CreateFromTask<Playlist>(async _ => await LoadPlaylist(_));
            InitPlaylistItemsCommand.IsExecuting.Subscribe(_ => IsPlaylistItemsBusy = _);

            // detail
            this.WhenAnyValue(vm => vm.SelectedPlaylistItem)
                .NotNull()
                .Select(_ => _.Item)
                .Cast<IDetailEntity>()
                .ToProperty(this, _ => _.CurrentDetailEntity, out currentDetailEntity);

            // add to playlist
            AddPlaylistCommand = ReactiveCommand.CreateFromTask(async _ => await AddPlaylist());
            AddToPlaylist = ReactiveCommand.CreateFromTask<Playlist>(async _ => await AddEpisodeToPlaylist(_));

            // remove from playlist
            RemoveFromPlaylist = ReactiveCommand.CreateFromTask(async _ => await RemoveItemFromPlaylist());

            // delete playlist
            DeletePlaylistCommand = ReactiveCommand.CreateFromTask<Playlist>(async _ => await DeletePlaylist(_));

            // play
            PlayItemCommand = ReactiveCommand.Create<IPlayable>(_ => OnPlayItem());

            // bring into view
            ScrollIntoView = ReactiveCommand.Create<int, int>(_ => _,
                this.WhenAny(_ => _.Player.Playlist.Current, _ => _ != null));

            // sort order
            PlaylistItemOrderBy.OnNext(new IOrderByExpression<PlaylistItem>[]
            {
                new OrderByExpression<PlaylistItem, int>(e => e.OrderNumber)
            });

            // locate current track
            this.WhenAnyValue(vm => vm.Player.LocateCurrent)
                .NotNull()
                .Subscribe(c => c.Subscribe(_ => LocatePageForCurrent()));

            // selected playlist changed
            this.WhenAnyValue(_ => _.SelectedPlaylist)
                .NotNull()
                .DistinctUntilChanged()
                .Subscribe(async _ => await OnSelectedPlaylist(_));

            // on playlists changed
            this.WhenAnyValue(_ => _.Playlists).NotNull().Subscribe(_ => OnPlaylistsChanged());

            // detail
            ToggleShowDetailCommand = ReactiveCommand.Create<bool, bool>(_ => _);
            // deactivation
            DeactivateCommand = ReactiveCommand.Create(() => { });
        }

        private void LocatePageForCurrent()
        {
            var index = Player.Playlist.Current.OrderNumber;

            ScrollIntoView.Execute(index).Subscribe();
        }

        private async Task OnSelectedPlaylist(Playlist playlist)
        {
            // selected playlist
            LastSelectedPlaylistId = SelectedPlaylist.Id;

            // init playlist items
            await InitPlaylistItemsCommand.Execute(playlist);
        }

        private void OnPlaylistsChanged()
        {
            PlaylistView = Playlists.CreateDerivedCollection(_ => _,
                orderer: OrderedComparer<Playlist>.OrderBy(_ => _.OrderNumber).Compare);
            ResetSelectedPlaylist();
        }

        private void ResetSelectedPlaylist()
        {
            var lastSelected = PlaylistView.FirstOrDefault(p => p.Id == LastSelectedPlaylistId);
            if (lastSelected == null && PlaylistView.Any())
            {
                lastSelected = PlaylistView.First();
            }

            SelectedPlaylist = lastSelected;
            SelectedPlaylistItem = SelectedPlaylist?.Current;
        }

        public ReactiveCommand<Playlist, Unit> InitPlaylistItemsCommand { get; }

        public ReactiveCommand<Unit, IPlaylist> AddPlaylistCommand { get; }
        public ReactiveCommand<Playlist, Unit> AddToPlaylist { get; }
        public IDetailEntity CurrentDetailEntity => currentDetailEntity.Value;
        public ReactiveCommand<Playlist, Unit> DeletePlaylistCommand { get; }
        public IScreen HostScreen { get; }
        public ReactiveCommand<Unit, Unit> GetPlaylistsCommand { get; }

        public bool IsDetailVisible
        {
            get => isDetailVisible;
            set => this.RaiseAndSetIfChanged(ref isDetailVisible, value);
        }

        public ReactiveCommand<bool, bool> ToggleShowDetailCommand { get; }

        private bool isDetailVisible;
        private bool suppressStoreSignal;

        public bool IsPlaylistItemsBusy
        {
            get => isPlaylistItemsBusy;
            set => this.RaiseAndSetIfChanged(ref isPlaylistItemsBusy, value);
        }

        public bool IsPlaylistsBusy
        {
            get => isPlaylistsBusy;
            set => this.RaiseAndSetIfChanged(ref isPlaylistsBusy, value);
        }

        public IPlayerModel Player { get; set; }
        public ReactiveCommand<IPlayable, Unit> PlayItemCommand { get; }

        public string PlaylistItemFilterText
        {
            get => playlistItemFilterText;
            set
            {
                var text = value?.Trim();
                this.RaiseAndSetIfChanged(ref playlistItemFilterText, text);
            }
        }

        public ReplaySubject<IOrderByExpression<PlaylistItem>[]> PlaylistItemOrderBy
        {
            get
                =>
                    playlistItemOrderBy
                    ?? (playlistItemOrderBy = new ReplaySubject<IOrderByExpression<PlaylistItem>[]>(1));
            set => this.RaiseAndSetIfChanged(ref playlistItemOrderBy, value);
        }

        public IReactiveDerivedList<PlaylistItem> PlaylistItems
        {
            get => playlistItems;
            set => this.RaiseAndSetIfChanged(ref playlistItems, value);
        }

        public IReactiveDerivedList<Playlist> PlaylistView
        {
            get => playlistView;
            set => this.RaiseAndSetIfChanged(ref playlistView, value);
        }

        public ReactiveList<Playlist> Playlists
        {
            get => playlists;
            set => this.RaiseAndSetIfChanged(ref playlists, value);
        }

        public ReactiveCommand<Unit, int> RemoveFromPlaylist { get; }

        public ReactiveCommand<int, int> ScrollIntoView { get; set; }

        public Playlist SelectedPlaylist
        {
            get => selectedPlaylist;
            set => this.RaiseAndSetIfChanged(ref selectedPlaylist, value);
        }

        public int LastSelectedPlaylistId
        {
            get => lastSelectedPlaylistId;
            private set => this.RaiseAndSetIfChanged(ref lastSelectedPlaylistId, value);
        }

        public PlaylistItem SelectedPlaylistItem
        {
            get => selectedPlaylistItem;
            set => this.RaiseAndSetIfChanged(ref selectedPlaylistItem, value);
        }

        public string UrlPathSegment => GetType().FullName;

        public async Task<IPlaylist> AddPlaylist()
        {
            var playlist = new Playlist("new playlist");

            suppressStoreSignal = true;
            var result = await playlistStore.Add(playlist);
            suppressStoreSignal = false;

            if (result > 0)
            {
                Playlists.Add(playlist);
            }

            return playlist;
        }

        public async Task<int> MoveItemToIndex(Playlist source, int index)
        {
            var result = await playlistStore.ChangeOrderAsync(source, index);

            if (result > 0)
            {
                PlaylistView.Reset();
            }

            return result;
        }

        public async Task<int> MoveItemToIndex(PlaylistItem source, int index)
        {
            var result = await playlistStore.ChangeItemOrderAsync(source, index, _ => _.PlaylistId == source.PlaylistId);

            if (result > 0)
            {
                PlaylistItems.Reset();
            }

            return result;
        }

        private async Task<int> AddEpisodeToPlaylist(Playlist playlist)
        {
            return await playlistStore.AddItem(SelectedPlaylistItem.Item.Id, playlist);
        }

        private async Task DeletePlaylist(Playlist playlist)
        {
            if (playlist == null)
            {
                throw new ArgumentNullException(nameof(playlist));
            }

            // TODO confirm
            var result = await playlistStore.Remove(playlist);

            if (result > 0)
            {
                Playlists.Remove(playlist);
                this.Log().Debug($"{playlist} deleted.");

                if (SelectedPlaylist == null)
                {
                    SelectedPlaylist = PlaylistView.FirstOrDefault();
                }
            }
        }

        private async Task LoadPlaylist(Playlist playlist)
        {
            await playlistStore.LoadSingleAsync(playlist);
            PlaylistItems = playlist.Items.CreateDerivedCollection(i => i,
                orderer: OrderedComparer<PlaylistItem>.OrderBy(pi => pi.OrderNumber).Compare);
        }

        private async Task GetPlaylists()
        {
            var managedPlaylists = await playlistStore.GetAllAsync(_ => _.Type == PlaylistType.User);

            Playlists = new ReactiveList<Playlist>(managedPlaylists)
            {
                ChangeTrackingEnabled = true
            };
        }

        private void OnPlayItem()
        {
            SetPlaylist();
            Player.Play();
        }

        private async Task<int> RemoveItemFromPlaylist()
        {
            if (!SelectedPlaylist.Items.Contains(SelectedPlaylistItem))
            {
                return 0;
            }

            var playlist = SelectedPlaylistItem.Playlist;
            if (Equals(SelectedPlaylistItem, playlist.Current))
            {
                playlist.Current = null;
            }

            var result = await playlistStore.RemoveItem(SelectedPlaylistItem);

            if (result > 0)
            {
                // TODO ?
            }

            return result;
        }

        private void SetPlaylist()
        {
            if (SelectedPlaylist == null)
            {
                return;
            }

            if (!Equals(Player.Playlist, SelectedPlaylist))
            {
                Player.Playlist = SelectedPlaylist;
            }

            if (Equals(SelectedPlaylist.Current, SelectedPlaylistItem))
            {
                SelectedPlaylist.SetActive();
            }
            else
            {
                SelectedPlaylist.Current = SelectedPlaylistItem;
            }
        }

        public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }

        public void Deactivate()
        {
            Player.Deactivate();
        }

        public void Start()
        {
            InitPlaylists();
        }

        private void InitPlaylists()
        {
            playlistStore.ResetSignal.StartWith(Unit.Default)
                .Where(_ => !suppressStoreSignal)
                .Subscribe(async _ => await GetPlaylists());
        }
    }
}