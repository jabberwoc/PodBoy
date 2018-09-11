using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using PodBoy.Resource.Converter;
using PodBoy.ViewModel;
using ReactiveUI;
using Splat;

namespace PodBoy.View
{
    /// <summary>
    /// Interaction logic for PlayerView.xaml
    /// </summary>
    public partial class PlayerView : IViewFor<PlayerViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
            typeof(PlayerViewModel), typeof(PlayerView), new UIPropertyMetadata());

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool),
            typeof(PlayerView), new UIPropertyMetadata(default(bool)));

        public PlayerViewModel ViewModel
        {
            get => (PlayerViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public bool IsBusy
        {
            get => (bool) GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public PlayerView()
        {
            InitializeComponent();

            ViewModel = Locator.Current.GetService<PlayerViewModel>();

            //TrackPlaceholder.Text = "PodBoy";

            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, _ => _.DataContext));

                d(this.BindCommand(ViewModel, vm => vm.TogglePlayCommand, v => v.Play));
                d(this.BindCommand(ViewModel, vm => vm.PlayPrevious, v => v.Previous));
                d(this.BindCommand(ViewModel, vm => vm.PlayNext, v => v.Next));
                d(this.BindCommand(ViewModel, vm => vm.LocateCurrent, v => v.Locate));

                d(this.OneWayBind(ViewModel, vm => vm.PlaceholderText, v => v.SliderPlaceholder.Text));

                d(this.Bind(ViewModel, vm => vm.IsPlaying, v => v.Play.IsChecked));

                d(this.OneWayBind(ViewModel, vm => vm.IsBusy, x => x.IsBusy));

                d(this.OneWayBind(ViewModel, vm => vm.Title, x => x.Title.Text));

                d(this.WhenAnyValue(_ => _.MediaSlider.Value)
                    .BindTo(this, _ => _.Position.Text, true, new TimeSpanBindingTypeConverter()));

                d(this.OneWayBind(ViewModel, vm => vm.TotalTime, v => v.Length.Text, true,
                    new TimeSpanBindingTypeConverter()));

                // player media
                d(ViewModel.MediaOpened.Subscribe(_ => InitMedia()));

                d(this.WhenAnyValue(_ => _.MediaSlider).Subscribe(_ => InitSlider()));

                d(this.Bind(ViewModel, vm => vm.Volume, v => v.Volume.CurrentVolume));

                // placeholder visibility if no track in player
                d(
                    this.WhenAny(_ => _.ViewModel.Playlist.Current, v => v.Value != null)
                        .StartWith(false)
                        .DistinctUntilChanged()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(OnTrackSelected));
            });
        }

        private void OnTrackSelected(bool isTrackSelected)
        {
            var visibility = ConvertTrackVisibilityFor(isTrackSelected);

            PlayerControls.Visibility = visibility;
            EpisodeImage.Visibility = visibility;

            TrackInfoGrid.Visibility = visibility;

            TrackPlaceholder.Visibility = ConvertTrackVisibilityFor(!isTrackSelected);
        }

        private Visibility ConvertTrackVisibilityFor(bool value)
        {
            return value ? Visibility.Visible : Visibility.Hidden;
        }

        private void InitMedia()
        {
            InitMediaComponents();

            MediaSlider.Maximum = ViewModel.TotalTime.TotalSeconds;
        }

        private void InitSlider()
        {
            InitSliderTrack();

            this.WhenAnyValue(_ => _.MediaSlider.Visibility)
                .Subscribe(
                    v => SliderPlaceholder.Visibility = v == Visibility.Visible ? Visibility.Hidden : Visibility.Visible);
        }

        private void InitSliderTrack()
        {
            var track = (Track) MediaSlider.Template.FindName("PART_Track", MediaSlider);

            var thumb = track.Thumb;

            // player position direction
            Observable.Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Where(_ => ViewModel.IsPlaying && !thumb.IsDragging)
                .Subscribe(_ => ViewModel.UpdatePositionFromPlayer());

            // slider direction
            Observable.FromEventPattern<DragCompletedEventHandler, DragCompletedEventArgs>(
                h => thumb.DragCompleted += h, h => thumb.DragCompleted -= h)
                .Subscribe(_ => ViewModel.UpdatePositionFromSlider(MediaSlider.Value));
        }

        private void InitMediaComponents()
        {
            Position.Visibility = Visibility.Visible;
            MediaSlider.Visibility = Visibility.Visible;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DataContext = ViewModel;

            this.WhenAnyValue(_ => _.ViewModel.StreamPosition)
                .Subscribe(_ => MediaSlider.Value = ViewModel.StreamPosition.TotalSeconds);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (PlayerViewModel) value;
        }
    }
}