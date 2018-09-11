using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FirstFloor.ModernUI.Windows.Controls;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Entity.Store;
using PodBoy.Feed;
using PodBoy.Notification;
using PodBoy.Properties;
using PodBoy.Virtualization;
using PodBoy.Extension;
using PodBoy.Playlists;
using Reactive.EventAggregator;
using ReactiveUI;
using Splat;

namespace PodBoy.ViewModel
{
    public class PodcastViewModel : ReactiveViewModel, IRoutableViewModel, IOrderedListManager<Channel>,
                                    IPlayableViewModel, IDeactivatable
    {
        private readonly IChannelStore channelStore;
        private readonly IPlaylistStore playlistStore;

        private ReactiveList<int> busyList;
        private ReactiveList<Channel> channels = new ReactiveList<Channel>();
        private ReactiveList<int> channelsBusyList;
        private readonly ObservableAsPropertyHelper<IDetailEntity> currentDetailEntity;

        private string episodeFilterText = string.Empty;
        private PagedList<Episode> episodeList;
        private ReplaySubject<IOrderByExpression<Episode>[]> episodeOrderBy;
        private bool isChannelsBusy;
        private bool isEpisodesBusy;
        private bool isUpdateEnabled = true;
        private bool isUpdating;
        private ReactiveList<IPlaylist> playlists;
        private readonly ObservableAsPropertyHelper<int> selectedChannelId;
        private Channel selectedChannelValue;
        private EpisodeSortOrder selectedEpisodeSortOrder = EpisodeSortOrder.Date;
        private Episode selectedEpisodeValue;
        private bool urlDialogIsOpen;
        private IReactiveDerivedList<Channel> channelView;

        private bool isDetailVisible;
        private bool isSortOrderDescending;

        public PodcastViewModel(IScreen hostScreen,
            IPlayerModel player,
            IEventAggregator eventAggregator,
            IDialogService dialogService,
            IFeedParser feedParser,
            IChannelStore channelStore,
            IPlaylistStore playlistStore)
            : base(eventAggregator)
        {
            HostScreen = hostScreen;
            Player = player;
            DialogService = dialogService;
            FeedParser = feedParser;

            this.channelStore = channelStore;
            this.playlistStore = playlistStore;

            GetChannelsCommand = ReactiveCommand.CreateFromTask(async _ => await GetChannels());
            GetChannelsCommand.IsExecuting.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnChannelsBusy(GetChannelsCommand.GetHashCode(), _));

            PlayItemCommand = ReactiveCommand.CreateFromTask<IPlayable>(async _ => await OnPlayEpisode(_ as Episode));

            // open url
            OpenUrlDialogCommand = ReactiveCommand.CreateFromTask(async _ => await OpenUrlDialog());
            OpenUrlDialogCommand.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LogAndNotify(NotificationType.Error, Messages.ERR_CHANNEL_LOAD));

            // channels busy indicator
            this.WhenAnyObservable(_ => _.ChannelsBusyList.CountChanged).DistinctUntilChanged()
                .Subscribe(_ => IsChannelsBusy = _ > 0);

            // episodes busy indicator
            this.WhenAnyObservable(_ => _.EpisodesBusyList.CountChanged).DistinctUntilChanged()
                .Subscribe(_ => IsEpisodesBusy = _ > 0);

            OpenUrlCommand = ReactiveCommand.CreateFromTask<string>(async _ => await OnOpenUrl(_));
            OpenUrlCommand.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LogAndNotify(NotificationType.Error, Messages.ERR_CHANNEL_LOAD));

            // load channel
            LoadChannelFromUrlCommand = ReactiveCommand.CreateFromTask<string>(LoadChannelFromUrlAsync);
            OpenUrlCommand.IsExecuting.ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ =>
            {
                OnChannelsBusy(OpenUrlCommand.GetHashCode(), _);
                OnEpisodesBusy(OpenUrlCommand.GetHashCode(), _);
            });

            LoadChannelFromUrlCommand.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LogAndNotify(NotificationType.Error, Messages.ERR_CHANNEL_LOAD));

            // delete channel
            ConfirmDeleteChannelCommand = ReactiveCommand.CreateFromTask<Channel>(async _ => await ConfirmDelete(_));
            DeleteChannelCommand = ReactiveCommand.CreateFromTask<Channel>(async _ => await DeleteChannel(_));
            DeleteChannelCommand.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LogAndNotify(NotificationType.Error, Messages.ERR_CHANNEL_DELETE));
            DeleteChannelCommand.IsExecuting.Subscribe(_ =>
            {
                OnChannelsBusy(DeleteChannelCommand.GetHashCode(), _);
                OnEpisodesBusy(DeleteChannelCommand.GetHashCode(), _);
            });

            var existsSelectedChannel = this.WhenAny(vm => vm.SelectedChannel, _ => SelectedChannel != null);
            this.WhenAnyValue(vm => vm.SelectedChannel).Select(_ => _ == null ? 0 : SelectedChannel.Id)
                .ToProperty(this, _ => _.SelectedChannelId, out selectedChannelId);
            RemoveFilterCommand = ReactiveCommand.Create<Unit>(_ => SelectedChannel = null, existsSelectedChannel);

            MarkAllPlayedCommand = ReactiveCommand.CreateFromTask<Channel>(async _ => await MarkChannelPlayed(_));

            CopyUrlCommand = ReactiveCommand.Create<Channel>(_ => Clipboard.SetText(_.Link));

            var episodeSet = this.WhenAnyValue(_ => _.SelectedEpisode);

            // detail
            this.WhenAnyValue(vm => vm.SelectedChannel).NotNull().Cast<IDetailEntity>()
                .Merge(episodeSet.Cast<IDetailEntity>())
                .ToProperty(this, _ => _.CurrentDetailEntity, out currentDetailEntity);

            this.WhenAnyValue(vm => vm.Player.LocateCurrent).NotNull()
                .Subscribe(c => c.Subscribe(_ => LocatePageForCurrent()));

            // episode list is loading
            this.WhenAnyValue(_ => _.EpisodeList.IsLoading).SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnEpisodesBusy(EpisodeList.GetHashCode(), _));

            ScrollIntoView = ReactiveCommand.Create<int, int>(_ => _);

            // playlist
            AddToPlaylist = ReactiveCommand.CreateFromTask<Playlist>(async _ => await AddEpisodeToPlaylist(_));

            // sort order
            this.WhenAnyValue(_ => _.SelectedEpisodeSortOrder)
                .Subscribe(_ => EpisodeOrderBy.OnNext(GetEpisodeSortOrder(_)));
            ToggleSortDirectionCommand =
                ReactiveCommand.Create(
                    () => EpisodeOrderBy.OnNext(GetEpisodeSortOrder(SelectedEpisodeSortOrder, false)));

            // on channels changed
            this.WhenAnyValue(_ => _.Channels).NotNull().Subscribe(_ => OnChannelsChanged());

            // update channels
            var canUpdate = new BehaviorSubject<bool>(false);
            UpdateChannelsCommand = ReactiveCommand.CreateFromTask(async _ => await UpdateChannelsAsync(),
                canUpdate.DistinctUntilChanged());

            UpdateChannelsCommand.IsExecuting.CombineLatest(this.WhenAnyValue(_ => _.Channels.IsEmpty),
                    (exec, empty) => !exec && !empty && IsUpdateEnabled).DistinctUntilChanged()
                .Subscribe(_ => canUpdate.OnNext(_));

            UpdateChannelsCommand.IsExecuting.ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => IsUpdating = _);
            UpdateChannelsCommand.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LogAndNotify(NotificationType.Error, Messages.ERR_CHANNEL_UPDATE));

            // init episodes
            EpisodeList = InitEpisodeList();
            EpisodeList.Changed.Subscribe(_ => InitActivePlaylist());

            // detail
            ToggleShowDetailCommand = ReactiveCommand.Create<bool, bool>(_ => _);
            DeactivateCommand = ReactiveCommand.Create(() => { });
        }

        public async Task StartAsync()
        {
            // init channels
            await InitChannels();

            // init playlists
            InitPlaylists();

            // update channels timer
            UpdateChannelsCommand.CanExecute.Where(_ => _).Take(1).Subscribe(_ => InitUpdateTimer());

            InitActivePlaylist();
        }

        public async Task InitChannels()
        {
            await GetChannels();
        }

        public ReactiveCommand<Playlist, Unit> AddToPlaylist { get; }

        public ReactiveList<Channel> Channels
        {
            get => channels;
            set => this.RaiseAndSetIfChanged(ref channels, value);
        }

        public IReactiveDerivedList<Channel> ChannelView
        {
            get => channelView;
            set => this.RaiseAndSetIfChanged(ref channelView, value);
        }

        public ReactiveList<int> ChannelsBusyList
        {
            get => channelsBusyList ?? (channelsBusyList = new ReactiveList<int>());
            set => this.RaiseAndSetIfChanged(ref channelsBusyList, value);
        }

        public bool IsDetailVisible
        {
            get => isDetailVisible;
            set => this.RaiseAndSetIfChanged(ref isDetailVisible, value);
        }

        public ReactiveCommand<bool, bool> ToggleShowDetailCommand { get; }

        public ReactiveCommand<Channel, Unit> CopyUrlCommand { get; set; }
        public IDetailEntity CurrentDetailEntity => currentDetailEntity.Value;
        public ReactiveCommand<Channel, Unit> DeleteChannelCommand { get; set; }
        public ReactiveCommand<Channel, Unit> ConfirmDeleteChannelCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ToggleSortDirectionCommand { get; set; }

        public IDialogService DialogService { get; set; }

        public string EpisodeFilterText
        {
            get => episodeFilterText;
            set
            {
                var text = value?.Trim();
                this.RaiseAndSetIfChanged(ref episodeFilterText, text);
            }
        }

        public PagedList<Episode> EpisodeList
        {
            get => episodeList;
            set => this.RaiseAndSetIfChanged(ref episodeList, value);
        }

        public ReplaySubject<IOrderByExpression<Episode>[]> EpisodeOrderBy
        {
            get => episodeOrderBy ?? (episodeOrderBy = new ReplaySubject<IOrderByExpression<Episode>[]>(1));
            set => this.RaiseAndSetIfChanged(ref episodeOrderBy, value);
        }

        public ReactiveList<int> EpisodesBusyList
        {
            get => busyList ?? (busyList = new ReactiveList<int>());
            set => this.RaiseAndSetIfChanged(ref busyList, value);
        }

        public IEnumerable<EpisodeSortOrder> EpisodeSortOrders { get; } =
            Enum.GetValues(typeof(EpisodeSortOrder)).Cast<EpisodeSortOrder>();

        public IFeedParser FeedParser { get; set; }
        public IScreen HostScreen { get; }

        public bool IsChannelsBusy
        {
            get => isChannelsBusy;
            private set => this.RaiseAndSetIfChanged(ref isChannelsBusy, value);
        }

        public bool IsEpisodesBusy
        {
            get => isEpisodesBusy;
            private set => this.RaiseAndSetIfChanged(ref isEpisodesBusy, value);
        }

        public bool IsUpdateEnabled
        {
            get => isUpdateEnabled;
            set => this.RaiseAndSetIfChanged(ref isUpdateEnabled, value);
        }

        public bool IsUpdating
        {
            get => isUpdating;
            set => this.RaiseAndSetIfChanged(ref isUpdating, value);
        }

        public ReactiveCommand<string, Unit> LoadChannelFromUrlCommand { get; set; }
        public ReactiveCommand<Channel, Unit> MarkAllPlayedCommand { get; set; }
        public ReactiveCommand<string, Unit> OpenUrlCommand { get; set; }
        public ReactiveCommand<Unit, Unit> OpenUrlDialogCommand { get; set; }
        public IPlayerModel Player { get; set; }
        public ReactiveCommand<IPlayable, Unit> PlayItemCommand { get; }

        public ReactiveList<IPlaylist> Playlists
        {
            get => playlists;
            set => this.RaiseAndSetIfChanged(ref playlists, value);
        }

        public ReactiveCommand<Unit, Unit> RemoveFilterCommand { get; set; }

        public ReactiveCommand<int, int> ScrollIntoView { get; }

        public Channel SelectedChannel
        {
            get => selectedChannelValue;
            set => this.RaiseAndSetIfChanged(ref selectedChannelValue, value);
        }

        public int SelectedChannelId => selectedChannelId.Value;

        public Episode SelectedEpisode
        {
            get => selectedEpisodeValue;
            set => this.RaiseAndSetIfChanged(ref selectedEpisodeValue, value);
        }

        public EpisodeSortOrder SelectedEpisodeSortOrder
        {
            get => selectedEpisodeSortOrder;
            set => this.RaiseAndSetIfChanged(ref selectedEpisodeSortOrder, value);
        }

        public bool IsSortOrderDescending
        {
            get => isSortOrderDescending;
            set => this.RaiseAndSetIfChanged(ref isSortOrderDescending, value);
        }

        public virtual IObservable<long> Timer { get; private set; }

        public CancellationTokenSource UpdateCancelToken { get; set; }

        public ReactiveCommand<Unit, Unit> UpdateChannelsCommand { get; set; }

        public TimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMinutes(10);

        public bool UrlDialogIsOpen
        {
            get => urlDialogIsOpen;
            set => this.RaiseAndSetIfChanged(ref urlDialogIsOpen, value);
        }

        public string UrlPathSegment => GetType().FullName;

        public string ViewModelInstanceId { get; set; }

        public virtual async Task DeleteChannel(Channel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            await playlistStore.RemoveItemsByChannel(channel);

            // remove channel
            var result = await channelStore.Remove(channel);

            if (result > 0)
            {
                Channels.Remove(channel);
                LogAndNotify(NotificationType.Info, $"Channel [{channel.Title}] deleted", "Channel deleted");

                if (SelectedChannel == null)
                {
                    EpisodeList.Clear();
                }
            }
        }

        public virtual void InitUpdateTimer()
        {
            Timer = Observable.Timer(TimeSpan.FromSeconds(0), UpdatePeriod, RxApp.MainThreadScheduler);
            Timer.Where(_ => IsUpdating == false).SubscribeOn(RxApp.MainThreadScheduler).Subscribe(_ =>
            {
                if (((ICommand) UpdateChannelsCommand).CanExecute(null))
                {
                    UpdateChannelsCommand.Execute().Subscribe();
                }
            });
        }

        public async Task<Channel> LoadChannelFromUrl(string uriString)
        {
            if (string.IsNullOrEmpty(uriString))
            {
                this.Log().Error(Messages.ERR_URI_EMPTY);
                return null;
            }

            var feed = await LoadFeedAsync(uriString);

            var channel = FeedParser.ParseChannelInfo(feed, uriString);
            var episodes = FeedParser.ParseItems(feed.ItunesItems, channel);

            channel.Episodes.AddRange(episodes);
            return channel;
        }

        public async Task<int> MoveItemToIndex(Channel source, int index)
        {
            var result = await channelStore.ChangeOrderAsync(source, index);

            if (result == 0)
            {
                return result;
            }

            ChannelView.Reset();

            return result;
        }

        public async Task OnOpenUrl(string uri)
        {
            if (Channels.Any(_ => _.Link == uri))
            {
                LogAndNotify(NotificationType.Error, @"Channel already present");
                return;
            }

            await LoadChannelFromUrlCommand.Execute(uri);
        }

        public void InitActivePlaylist()
        {
            if (Player.Playlist != null)
            {
                if (Player.Playlist.Type != PlaylistType.Podcast)
                {
                    return;
                }

                if (Player.Playlist.Current == null)
                {
                    return;
                }

                var itemId = Player.Playlist.Current.ItemId;
                var toSelect = EpisodeList.Find(itemId);
                if (toSelect == null)
                {
                    this.Log().LogForType(NotificationType.Error, $"Episode {itemId} not found in EpisodeList.");
                    return;
                }
                CreatePodcastPlaylist(toSelect);
                SelectedEpisode = toSelect;
            }
            else
            {
                this.WhenAnyObservable(_ => _.EpisodeList.IsEmptyChanged).Where(_ => _ == false).Take(1).Subscribe(_ =>
                {
                    var toSelect = EpisodeList.CachedItems.FirstOrDefault();

                    if (toSelect != null)
                    {
                        SelectedEpisode = CreatePodcastPlaylist(toSelect);
                    }
                });
            }
        }

        public ReactiveCommand<Unit, Unit> GetChannelsCommand { get; set; }

        private void OnChannelsChanged()
        {
            ChannelView = Channels.CreateDerivedCollection(_ => _,
                orderer: OrderedComparer<Channel>.OrderBy(_ => _.OrderNumber).Compare);
            ResetSelectedChannel();

            EpisodeList?.Clear();
        }

        private void ResetSelectedChannel()
        {
            SelectedChannel = SelectedChannelId != 0
                ? ChannelView.FirstOrDefault(p => p.Id == SelectedChannelId)
                : null;
        }

        public virtual async Task<IEnumerable<Episode>> UpdateChannel(ChannelInfo channelInfo)
        {
            if (channelInfo.FeedUrl == null)
            {
                throw new ArgumentException("FeedUrl is null");
            }

            var feed = await LoadFeedAsync(channelInfo.FeedUrl);

            if (feed == null)
            {
                return Enumerable.Empty<Episode>();
            }

            var episodes = FeedParser.ParseItems(feed.ItunesItems, channelInfo);

            return episodes;
        }

        public virtual async Task<IList<Episode>> UpdateChannels()
        {
            this.Log().Info(Messages.INF_CHANNELS_UPDATE);

            var updates = new List<Episode>();
            if (UpdateCancelToken.Token.IsCancellationRequested)
            {
                return updates;
            }

            var channelInfos = await channelStore.GetChannelInfos();

            foreach (var channelInfo in channelInfos)
            {
                if (UpdateCancelToken.Token.IsCancellationRequested)
                {
                    return updates;
                }
                updates.AddRange(await UpdateChannel(channelInfo));
                if (UpdateCancelToken.Token.IsCancellationRequested)
                {
                    return updates;
                }
            }
            return updates;
        }

        public async Task UpdateChannelsAsync()
        {
            UpdateCancelToken = new CancellationTokenSource();

            var updates = await UpdateChannels();

            if (updates.Any())
            {
                if (UpdateCancelToken.Token.IsCancellationRequested)
                {
                    this.Log().Info("Update canceled");
                    return;
                }
                await AddUpdates(updates);
                if (SelectedChannel == null || updates.Select(_ => _.ChannelId).Distinct()
                        .Any(_ => _ == SelectedChannel.Id))
                {
                    EpisodeList.Clear();
                }
            }

            if (updates.Count > 0)
            {
                LogAndNotify(NotificationType.Info,
                    "Update complete{0}{1} new episodes".FormatString(Environment.NewLine, updates.Count.ToString()));
            }

            UpdateCancelToken = null;
        }

        private async Task AddChannel(Channel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            var result = await channelStore.Add(channel);
            if (result > 0)
            {
                Channels.Add(channel);
                if (SelectedChannel == null)
                {
                    EpisodeList.Clear();
                }
            }
        }

        private async Task<int> AddEpisodeToPlaylist(Playlist playlist)
        {
            return await playlistStore.AddItem(SelectedEpisode.Id, playlist);
        }

        // TODO ReactiveUI Interactions
        private async Task ConfirmDelete(Channel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            var dialog = DialogService.RequestDeleteDialog(channel.Title);
            dialog.ShowDialog();

            if (dialog.MessageBoxResult == MessageBoxResult.Yes)
            {
                if (IsUpdating)
                {
                    // cancel update
                    UpdateCancelToken?.Cancel();
                }
                await DeleteChannelCommand.Execute(channel);
            }
        }

        private IOrderByExpression<Episode>[] GetEpisodeSortOrder(EpisodeSortOrder order, bool withDefaultOrder = true)
        {
            // TODO refactor
            bool isDescending;

            switch (order)
            {
                case EpisodeSortOrder.Date:
                    isDescending = GetSortDirection(true, withDefaultOrder);

                    return new IOrderByExpression<Episode>[]
                    {
                        new OrderByExpression<Episode, DateTime>(e => e.Date ?? DateTime.MinValue, isDescending)
                    };
                case EpisodeSortOrder.Channel:
                    isDescending = GetSortDirection(false, withDefaultOrder);

                    return new IOrderByExpression<Episode>[]
                    {
                        new OrderByExpression<Episode, int>(e => e.ChannelId, isDescending), // OrderNumber
                        new OrderByExpression<Episode, DateTime>(e => e.Date ?? DateTime.MinValue, true)
                    };
                case EpisodeSortOrder.Title:
                    isDescending = GetSortDirection(false, withDefaultOrder);
                    return new IOrderByExpression<Episode>[]
                    {
                        new OrderByExpression<Episode, string>(e => e.Title, isDescending)
                    };
                case EpisodeSortOrder.Played:
                    isDescending = GetSortDirection(false, withDefaultOrder);
                    return new IOrderByExpression<Episode>[]
                    {
                        new OrderByExpression<Episode, bool>(e => e.IsPlayed, isDescending),
                        new OrderByExpression<Episode, DateTime>(e => e.Date ?? DateTime.MinValue, true)
                    };
                default: throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        private bool GetSortDirection(bool defaultDescending, bool update)
        {
            var isDescending = update ? defaultDescending : IsSortOrderDescending;

            if (update)
            {
                IsSortOrderDescending = isDescending;
            }

            return isDescending;
        }

        private async Task GetPlaylists()
        {
            var managedPlaylists = await playlistStore.GetAllAsync(_ => _.Type == PlaylistType.User);
            Playlists = new ReactiveList<IPlaylist>(managedPlaylists);
        }

        private async Task GetChannels()
        {
            var managedChannels = await channelStore.GetAllAsync();
            Channels = new ReactiveList<Channel>(managedChannels)
            {
                ChangeTrackingEnabled = true
            };
        }

        private PagedList<Episode> InitEpisodeList()
        {
            Expression<Func<Episode, bool>> filterExpression =
                _ => (SelectedChannelId == 0 || SelectedChannelId == _.Channel.Id)
                     && (string.IsNullOrEmpty(EpisodeFilterText)
                         || _.Title.ToLower().Contains(EpisodeFilterText.ToLower()));

            var providerFilter = new ProviderFilter<Episode>(filterExpression,
                this.WhenAnyValue(_ => _.EpisodeFilterText).Select(_ => Unit.Default)
                    .Merge(this.WhenAnyValue(_ => _.SelectedChannelId).Select(_ => Unit.Default)));

            // include channel for title display. refactor to avoid stale entities ?
            var include = new Expression<Func<Episode, object>>[]
            {
                _ => _.Channel
            };

            var episodeProvider = new RepoProvider<Episode>(include, providerFilter, EpisodeOrderBy);
            return new PagedList<Episode>(episodeProvider, 50, 30000);
        }

        private void InitPlaylists()
        {
            playlistStore.ResetSignal.StartWith(Unit.Default).Subscribe(async _ => await GetPlaylists());
        }

        private async Task LoadChannelFromUrlAsync(string uriString)
        {
            var channel = await LoadChannelFromUrl(uriString);

            await AddChannel(channel);

            LogAndNotify(NotificationType.Info, $"Channel [{channel.Title}] added", "Channel added");
        }

        private async Task<ItunesFeed> LoadFeedAsync(string uri)
        {
            return await Observable.Start(() => FeedParser.LoadFeed(uri), RxApp.TaskpoolScheduler);
        }

        private void LocatePageForCurrent()
        {
            var index = EpisodeList.FetchIndexForItem(Player.Playlist.Current.Item);
            if (index != -1)
            {
                ScrollIntoView.Execute(index).Subscribe();
            }
        }

        private async Task MarkChannelPlayed(Channel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            if (channel.UnplayedCount == 0)
            {
                return;
            }

            await channelStore.MarkChannelPlayed(channel);

            if (SelectedChannel == null || Equals(SelectedChannel, channel))
            {
                EpisodeList.Clear();
            }
        }

        private void OnChannelsBusy(int sourceId, bool isSourceBusy)
        {
            if (isSourceBusy)
            {
                ChannelsBusyList.Add(sourceId);
            }
            else
            {
                ChannelsBusyList.Remove(sourceId);
            }
        }

        private void OnEpisodesBusy(int sourceId, bool isSourceBusy)
        {
            if (isSourceBusy)
            {
                EpisodesBusyList.Add(sourceId);
            }
            else
            {
                EpisodesBusyList.Remove(sourceId);
            }
        }

        private async Task OnPlayEpisode(Episode episode)
        {
            if (episode == null)
            {
                return;
            }

            bool updateUnplayed = !episode.IsPlayed;

            episode = CreatePodcastPlaylist(episode);

            Player.Play();

            if (updateUnplayed)
            {
                await channelStore.UpdateEpisode(episode);
            }
        }

        private async Task OpenUrlDialog()
        {
            var urlView = Locator.Current.GetService<IViewFor<OpenUrlViewModel>>();
            var dialog = new ModernDialog
            {
                Title = Messages.ADD_PODCAST,
                Content = urlView
            };
            dialog.Buttons = new[]
            {
                dialog.OkButton,
                dialog.CancelButton
            };
            dialog.CancelButton.IsCancel = true;

            urlView.ViewModel.WhenAnyValue(_ => _.IsValid).Subscribe(v => dialog.OkButton.IsEnabled = v);

            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var input = urlView.ViewModel.UrlInput;
                urlView.ViewModel.UrlInput = null;
                await OpenUrlCommand.Execute(input);
            }
        }

        private async Task<int> AddUpdates(IEnumerable<Episode> episodes)
        {
            return await channelStore.SaveEpisodes(episodes);
        }

        private Episode CreatePodcastPlaylist(Episode episode, IPlaylist playlist = null)
        {
            var source = playlist ?? playlistStore.CreatePodcastList(EpisodeList.CachedItems.ToArray());

            source.SetCurrent(episode);
            Player.Playlist = source;
            return episode;
        }

        public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }

        public void Deactivate()
        {
            DeactivateCommand.Execute();

            Player.Deactivate();
        }
    }
}