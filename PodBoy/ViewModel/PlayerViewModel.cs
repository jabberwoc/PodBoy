using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using CSCore.SoundOut;
using PodBoy.Entity;
using PodBoy.Entity.Store;
using PodBoy.Event;
using PodBoy.Properties;
using PodBoy.Extension;
using PodBoy.Notification;
using PodBoy.Playlists;
using Reactive.EventAggregator;
using ReactiveUI;

namespace PodBoy.ViewModel
{
    public class PlayerViewModel : ReactiveViewModel, IPlayerModel, IProvidesImage
    {
        private const int ImagePixelWidth = 40;
        private IPlaylist playlist;
        private bool isMuted;
        private bool isPlaying;
        private Uri mediaUri;

        private string placeholderText;

        private TimeSpan streamPosition;
        private string title;
        private TimeSpan totalTime;
        private double volume;
        private readonly IPlaylistStore playlistStore;
        private readonly IPlayer player;
        private readonly ReplaySubject<Unit> mediaOpened = new ReplaySubject<Unit>();
        private readonly ObservableAsPropertyHelper<bool> isBusy;

        public PlayerViewModel(IEventAggregator eventAggregator, IPlaylistStore playlistStore, IPlayer player)
            : base(eventAggregator)
        {
            this.playlistStore = playlistStore;
            this.player = player;

            // toggle play
            var canTogglePlay = this.WhenAnyValue(vm => vm.MediaUri).Select(_ => _ != null);
            TogglePlayCommand = ReactiveCommand.Create<Unit>(_ => TogglePlay(), canTogglePlay);
            TogglePlayCommand.ThrownExceptions.Subscribe(OnMediaFailed);

            // open media
            OpenMediaCommand = ReactiveCommand.CreateFromTask(async _ => await OpenMedia());
            OpenMediaCommand.IsExecuting.ToProperty(this, _ => _.IsBusy, out isBusy);

            // shortcut
            EventAggregator.GetEvent<ShortcutEvent>()
                .Where(_ => _.Type == ShortcutCommandType.TogglePlay)
                .Subscribe(_ => OnTogglePlayShortcut());

            //play next, previous
            PlayPrevious = ReactiveCommand.Create<Unit>(_ => Playlist.PlayPrevious(),
                this.WhenAny(vm => vm.Playlist.Previous, e => e.Value != null));
            PlayNext = ReactiveCommand.Create<Unit>(_ => Playlist.PlayNext(),
                this.WhenAny(vm => vm.Playlist.Next, e => e.Value != null));

            // playlist
            // TODO obsolete?
            ShowPlaylist = ReactiveCommand.Create<Unit>(_ => { },
                this.WhenAny(vm => vm.Playlist, p => p.Value != null && p.Value.AnyItems()));

            // volume
            MuteVolumeToggle = ReactiveCommand.Create<Unit>(_ => ToggleMute());

            this.WhenAnyValue(vm => vm.Volume).Skip(1).Subscribe(v => Settings.Default.PlayerVolume = v);
            this.WhenAnyValue(vm => vm.Volume).Subscribe(volume => IsMuted = volume <= 0);

            // reset active / playing on change
            this.WhenAnyValue(_ => _.Playlist).Previous().NotNull().Subscribe(_ => _.ClearActive());

            // current episode
            this.WhenAnyValue(vm => vm.Playlist.Current).WithPrevious().Subscribe(async _ =>
            {
                if (_.OldValue?.ItemId != _.NewValue?.ItemId)
                {
                    await UpdateElapsed(_.OldValue?.Item);
                }
                SelectEpisode();
            });

            // volume
            this.WhenAnyValue(_ => _.Volume).Subscribe(_ => player.Volume = _);

            // set playlist element playing
            this.WhenAnyValue(_ => _.IsPlaying)
                .CombineLatest(this.WhenAnyValue(_ => _.Playlist.Current), (isPlaying, _) => isPlaying)
                .Where(_ => Playlist.Current != null)
                .Subscribe(_ => Playlist.SetCurrentPlaying(_));

            // sync stream position
            this.WhenAnyValue(_ => _.StreamPosition)
                .Where(_ => Playlist?.Current != null)
                .Select(_ => _.TotalSeconds)
                .Subscribe(_ => Playlist.Current.Item.ElapsedSeconds = _);

            // locate
            LocateCurrent = ReactiveCommand.Create<Unit>(_ => { });

            this.WhenAnyValue(_ => _.MediaUri).NotNull().Subscribe(async _ => await OpenMediaCommand.Execute());

            // player stopped
            player.PlaybackStopped.ObserveOn(RxApp.MainThreadScheduler).Subscribe(OnStreamStopped);
        }

        public void UpdatePositionFromPlayer()
        {
            if (player.Length.TotalSeconds > 0)
            {
                if (!IsPlaying)
                {
                    return;
                }
                if (TotalTime.TotalSeconds > 0)
                {
                    StreamPosition = player.Position;
                }
            }
        }

        private async Task UpdateElapsed(Episode item)
        {
            if (item == null)
            {
                return;
            }

            await playlistStore.UpdateEpisodeAsync(item);
        }

        public IObservable<Unit> MediaOpened => mediaOpened;

        private void OnTogglePlayShortcut()
        {
            IsPlaying = !IsPlaying;
            TogglePlayCommand.Execute().Subscribe();
        }

        public IPlaylist Playlist
        {
            get => playlist;
            set => this.RaiseAndSetIfChanged(ref playlist, value);
        }

        public bool IsMuted
        {
            get => isMuted;
            set => this.RaiseAndSetIfChanged(ref isMuted, value);
        }

        public bool IsPlaying
        {
            get => isPlaying;
            set => this.RaiseAndSetIfChanged(ref isPlaying, value);
        }

        public bool IsBusy => isBusy.Value;

        public Uri MediaUri
        {
            get => mediaUri;
            set => this.RaiseAndSetIfChanged(ref mediaUri, value);
        }

        public ReactiveCommand<Unit, Unit> MuteVolumeToggle { get; }

        public ReactiveCommand<Unit, Unit> LocateCurrent { get; }

        public double OldVolume { get; set; }

        public string PlaceholderText
        {
            get => placeholderText;
            set => this.RaiseAndSetIfChanged(ref placeholderText, value);
        }

        public ReactiveCommand<Unit, Unit> PlayNext { get; }

        public ReactiveCommand<Unit, Unit> PlayPrevious { get; }

        public ReactiveCommand<Unit, Unit> ShowPlaylist { get; }

        public TimeSpan StreamPosition
        {
            get => streamPosition;
            set => this.RaiseAndSetIfChanged(ref streamPosition, value);
        }

        public string Title
        {
            get => title;
            set => this.RaiseAndSetIfChanged(ref title, value);
        }

        public ReactiveCommand<Unit, Unit> TogglePlayCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenMediaCommand { get; }

        public TimeSpan TotalTime
        {
            get => totalTime;
            set => this.RaiseAndSetIfChanged(ref totalTime, value);
        }

        public double Volume
        {
            get => volume;
            set
            {
                OldVolume = volume;
                this.RaiseAndSetIfChanged(ref volume, value);
            }
        }

        public void MuteVolume()
        {
            Volume = 0;
        }

        public void Play()
        {
            if (((ICommand) TogglePlayCommand).CanExecute(null))
            {
                TogglePlayCommand.Execute();
                IsPlaying = true;
            }
        }

        private async Task OpenMedia()
        {
            try
            {
                await player.Open(MediaUri).SubscribeOn(RxApp.TaskpoolScheduler).ObserveOn(RxApp.MainThreadScheduler);

                TotalTime = player.Length;

                var position = StreamPosition;
                player.Position = position;
                //StreamPosition = position;

                if (IsPlaying)
                {
                    player.Play();
                }

                mediaOpened.OnNext(Unit.Default);
            }
            catch (Exception e)
            {
                OnMediaFailed(e);
            }
        }

        private void TogglePlay()
        {
            if (IsPlaying)
            {
                if (StreamPosition == player.Length)
                {
                    ResetStreamPosition();
                }
                player.Play();
            }
            else
            {
                player.Pause();
            }
        }

        private void OnStreamStopped(PlaybackStoppedEventArgs e)
        {
            // play next at end of stream
            if (((ICommand) PlayNext).CanExecute(null))
            {
                ResetStreamPosition();
                PlayNext.Execute().Subscribe();
            }
            else
            {
                StopPlayer();
            }
        }

        private void StopPlayer()
        {
            player.Stop();
            IsPlaying = false;
            ResetStreamPosition();
        }

        private void ResetStreamPosition()
        {
            StreamPosition = TimeSpan.Zero;
        }

        public void RestoreVolume()
        {
            Volume = OldVolume > 0 ? OldVolume : 100;
        }

        public async Task StartAsync()
        {
            Volume = Settings.Default.PlayerVolume;

            var lastPlaylist = Settings.Default.ActivePlaylistId;
            var lastEpisode = Settings.Default.ActiveEpisodeId;

            if (lastPlaylist == 0 && lastEpisode == 0)
            {
                return;
            }

            if (lastPlaylist == 0)
            {
                var podcastList = await playlistStore.CreatePodcastList(lastEpisode.InArray());
                podcastList.Current = podcastList.Items.FirstOrDefault();
                Playlist = podcastList;

                return;
            }

            var targetList = await playlistStore.GetSingleAsync(lastPlaylist);
            if (targetList == null)
            {
                return;
            }

            await playlistStore.LoadSingleAsync(targetList);

            Playlist = targetList;
            SelectEpisode();
        }

        public void ToggleMute()
        {
            if (IsMuted)
            {
                MuteVolume();
            }
            else
            {
                RestoreVolume();
            }
        }

        private void SelectEpisode()
        {
            if (Playlist.Current == null)
            {
                PlaceholderText = string.Empty;

                ImageProvider.ImageUri = null;
                ImageProvider.ResetImage();
                MediaUri = null;
                Title = string.Empty;
                StreamPosition = TimeSpan.FromSeconds(0);

                return;
            }

            Contract.Assert(Playlist.Current.Item != null);
            if (MediaUri?.AbsoluteUri != Playlist.Current.Item?.Media?.Url)
            {
                SetMedia();

                PlaceholderText = GetPlaceholderText();
                ImageProvider.ImageUri = Playlist.Current.Item.ImageUri;
                ImageProvider.ResetImage();
            }

            Playlist.SetActive();
        }

        private string GetPlaceholderText()
        {
            if (StreamPosition == TimeSpan.Zero)
            {
                return Resources.Player_PlaceholderText_Start;
            }
            return Resources.Player_PlaceholderText_Resume.FormatString(StreamPosition.ToString(@"h\:mm\:ss"));
        }

        private void SaveSettings()
        {
            if (Playlist?.Current != null)
            {
                playlistStore.UpdateEpisode(Playlist.Current.Item);
            }

            Settings.Default.PlayerVolume = Volume;

            Settings.Default.ActiveEpisodeId = Playlist?.Current?.ItemId ?? 0;
            Settings.Default.ActivePlaylistId = Playlist?.Id ?? 0;
            Settings.Default.Save();
        }

        private void SetMedia()
        {
            Debug.Assert(Playlist.Current != null);

            if (string.IsNullOrEmpty(Playlist.Current.Item?.Media?.Url))
            {
                return;
            }
            var uriBuilder = new UriBuilder(Playlist.Current.Item?.Media?.Url);
            if (uriBuilder.Scheme == Uri.UriSchemeHttps)
            {
                // https not supported by player; try http
                uriBuilder.Scheme = Uri.UriSchemeHttp;
                uriBuilder.Port = -1;
            }
            var uri = uriBuilder.Uri;

            MediaUri = uri;
            Title = Playlist.Current.Item.Title;
            StreamPosition = TimeSpan.FromSeconds(Playlist.Current.Item.ElapsedSeconds);
        }

        public void OnMediaFailed(Exception e)
        {
            LogAndNotify(NotificationType.Error, Messages.PLAYER_MEDIA_FAILED.FormatString(e.Message));
            IsPlaying = false;
        }

        public IImageProvider ImageProvider { get; } = new ImageProvider(ImagePixelWidth);

        // TODO call separately
        public void Deactivate()
        {
            SaveSettings();
        }

        public void UpdatePositionFromSlider(double value)
        {
            if (TotalTime.TotalSeconds > 0)
            {
                player.Position = TimeSpan.FromSeconds(value);
            }
        }
    }
}