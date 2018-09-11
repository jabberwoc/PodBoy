using System;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PodBoy.ViewModel;
using ReactiveUI;
using EventsMixin = System.Windows.EventsMixin;

namespace PodBoy.View
{
    /// <summary>
    /// Interaction logic for PodcastView.xaml
    /// </summary>
    public partial class PodcastView : IViewFor<PodcastViewModel>, IViewWithPlayableViewModel
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(PodcastViewModel), typeof(PodcastView));

        public PodcastView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.EpisodeList, v => v.Episodes.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.ChannelView, v => v.Channels.ItemsSource));

                d(this.OneWayBind(ViewModel, vm => vm.IsEpisodesBusy, x => x.EpisodesBusyOverlay.Visibility));
                d(this.OneWayBind(ViewModel, vm => vm.IsChannelsBusy, x => x.ChannelsBusyOverlay.Visibility));
                d(this.Bind(ViewModel, vm => vm.SelectedEpisode, x => x.Episodes.SelectedItem));
                d(this.Bind(ViewModel, vm => vm.SelectedChannel, x => x.Channels.SelectedItem));
                d(this.BindCommand(ViewModel, vm => vm.OpenUrlDialogCommand, v => v.AddChannelButton));
                d(this.BindCommand(ViewModel, vm => vm.RemoveFilterCommand, v => v.RemoveFilterButton));
                d(this.BindCommand(ViewModel, vm => vm.UpdateChannelsCommand, v => v.Update));

                d(this.OneWayBind(ViewModel, vm => vm.UrlDialogIsOpen, v => v.OpenUrlOverlay.Visibility));

                d(this.OneWayBind(ViewModel, vm => vm.CurrentDetailEntity, v => v.Detail.DetailEntity));

                // placeholders
                d(this.WhenAnyValue(_ => _.Channels.Items.IsEmpty)
                    .CombineLatest(this.WhenAnyValue(_ => _.ViewModel.IsChannelsBusy), (x, y) => x && !y)
                    .DistinctUntilChanged().BindTo(this, _ => _.ChannelsPlaceholder.Visibility));

                d(this.WhenAnyValue(_ => _.Episodes.Items.IsEmpty)
                    .CombineLatest(this.WhenAnyValue(_ => _.ViewModel.IsEpisodesBusy), (x, y) => x && !y)
                    .DistinctUntilChanged().BindTo(this, _ => _.EpisodesPlaceholder.Visibility));

                d(this.WhenAnyValue(_ => _.ViewModel).BindTo(this, _ => _.DragDropBehavior.ListManager));

                // context menu is handled
                d(EventsMixin.Events(Channels).PreviewMouseRightButtonDown.Subscribe(e => e.Handled = true));

                // search
                d(this.WhenAnyValue(_ => _.SearchIcon.IsChecked).Subscribe(ToggleSearch));
                d(this.WhenAnyValue(_ => _.FilterTextBox.Text).Throttle(TimeSpan.FromSeconds(.5))
                    .BindTo(ViewModel, _ => _.EpisodeFilterText));

                d(this.WhenAnyValue(_ => _.SearchIcon.IsChecked).Subscribe(ToggleSearch));

                d(ViewModel.ScrollIntoView.Subscribe(BringEpisodeIntoView));

                // episode sort order
                d(this.OneWayBind(ViewModel, vm => vm.EpisodeSortOrders, v => v.EpisodeOrder.ItemsSource));
                d(this.Bind(ViewModel, vm => vm.SelectedEpisodeSortOrder, v => v.EpisodeOrder.SelectedItem));
                d(this.Bind(ViewModel, vm => vm.IsSortOrderDescending, v => v.SortDirection.IsChecked));
                d(this.BindCommand(ViewModel, _ => _.ToggleSortDirectionCommand, _ => _.SortDirection));

                // Detail
                // .. visibility
                d(this.WhenAnyValue(_ => _.DetailColumn.Width).Select(_ => _.Value > 5).DistinctUntilChanged()
                    .BindTo(ViewModel, _ => _.IsDetailVisible));
                d(this.OneWayBind(ViewModel, _ => _.IsDetailVisible, _ => _.Detail.Visibility));

                // toggle Detail button
                d(this.BindCommand(ViewModel, _ => _.ToggleShowDetailCommand, _ => _.ShowDetailButton,
                    _ => _.IsDetailVisible));
                // .. command
                d(this.WhenAnyObservable(_ => _.ViewModel.ToggleShowDetailCommand)
                    .BindTo(this, b => b.ColumnGridBehavior.ToggleDetail));
                // .. isChecked
                d(this.Bind(ViewModel, _ => _.IsDetailVisible, _ => _.ShowDetailButton.IsChecked));
                // .. isEnabled
                d(this.WhenAny(_ => _.Detail.DetailEntity, _ => _.Value != null).DistinctUntilChanged()
                    .BindTo(this, _ => _.ShowDetailButton.IsEnabled));
                // Detail splitter visibility
                d(this.WhenAny(_ => _.ViewModel.SelectedEpisode, _ => _.Value != null).DistinctUntilChanged()
                    .BindTo(this, _ => _.DetailSplitter.IsEnabled));

                // save column settings
                d(this.WhenAnyObservable(_ => _.ViewModel.DeactivateCommand)
                    .Subscribe(_ => ColumnGridBehavior.SaveSettings()));
            });
        }

        // TODO extension method
        private void BringEpisodeIntoView(int index)
        {
            var vsp = (VirtualizingStackPanel) typeof(ItemsControl).InvokeMember("_itemsHost",
                BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic, null, Episodes, null);

            var scrollHeight = vsp.ScrollOwner.ScrollableHeight;

            // itemIndex_ is index of the item which we want to show in the middle of the view
            var offset = scrollHeight * index / Episodes.Items.Count;

            vsp.SetVerticalOffset(offset);
        }

        private void ToggleSearch(bool? isChecked)
        {
            if (!isChecked.HasValue)
            {
                return;
            }

            var isOpen = isChecked.Value;

            FilterTextBox.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
            if (!isOpen)
            {
                FilterTextBox.Text = string.Empty;
            }
            else
            {
                // set focus
                FilterTextBox.Focus();
            }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (PodcastViewModel) value; }
        }

        public IPlayableViewModel PlayableViewModel => ViewModel;

        public PodcastViewModel ViewModel
        {
            get { return (PodcastViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}