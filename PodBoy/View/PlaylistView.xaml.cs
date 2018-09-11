using System;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PodBoy.Playlists;
using ReactiveUI;
using EventsMixin = System.Windows.EventsMixin;

namespace PodBoy.View
{
    /// <summary>
    /// Interaction logic for PlaylistView.xaml
    /// </summary>
    public partial class PlaylistView : IViewFor<PlaylistViewModel>, IViewWithPlayableViewModel
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
            typeof(PlaylistViewModel), typeof(PlaylistView));

        public PlaylistView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.PlaylistItems, v => v.PlaylistItems.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.PlaylistView, v => v.Playlists.ItemsSource));

                d(this.OneWayBind(ViewModel, vm => vm.SelectedPlaylist.Name, v => v.PlaylistItemsHeader.Text));

                // busy overlays
                d(this.OneWayBind(ViewModel, vm => vm.IsPlaylistsBusy, x => x.PlaylistsBusyOverlay.Visibility));
                d(this.OneWayBind(ViewModel, vm => vm.IsPlaylistItemsBusy, x => x.ItemsBusyOverlay.Visibility));

                // placeholders
                d(
                    this.WhenAnyValue(_ => _.Playlists.Items.IsEmpty)
                        .CombineLatest(this.WhenAnyValue(_ => _.ViewModel.IsPlaylistsBusy), (x, y) => x && !y)
                        .DistinctUntilChanged()
                        .BindTo(this, _ => _.PlaylistsPlaceholder.Visibility));

                d(
                    this.WhenAnyValue(_ => _.PlaylistItems.Items.IsEmpty)
                        .CombineLatest(this.WhenAnyValue(_ => _.ViewModel.IsPlaylistItemsBusy), (x, y) => x && !y)
                        .DistinctUntilChanged()
                        .BindTo(this, _ => _.PlaylistItemsPlaceholder.Visibility));

                d(this.Bind(ViewModel, vm => vm.SelectedPlaylistItem, _ => _.PlaylistItems.SelectedItem));
                d(this.Bind(ViewModel, vm => vm.SelectedPlaylist, _ => _.Playlists.SelectedItem));
                d(this.BindCommand(ViewModel, vm => vm.AddPlaylistCommand, v => v.AddPlaylistButton));
                //d(this.BindCommand(ViewModel, vm => vm.RemoveFilterCommand, v => v.RemoveFilterButton));

                d(this.OneWayBind(ViewModel, vm => vm.CurrentDetailEntity, v => v.Detail.DetailEntity));

                d(this.WhenAnyValue(_ => _.ViewModel).BindTo(this, _ => _.DragDropBehavior.ListManager));

                // context menu is handled
                d(EventsMixin.Events(Playlists).PreviewMouseRightButtonDown.Subscribe(e => e.Handled = true));

                // search
                d(this.WhenAnyValue(_ => _.SearchIcon.IsChecked).Subscribe(ToggleSearch));
                d(this.WhenAnyValue(_ => _.FilterTextBox.Text)
                    .Throttle(TimeSpan.FromSeconds(.5))
                    .BindTo(ViewModel, _ => _.PlaylistItemFilterText));

                //d(this.WhenAnyValue(_ => _.SearchIcon.IsChecked).Subscribe(ToggleSearch));

                d(ViewModel.ScrollIntoView.Subscribe(BringItemIntoView));

                // Detail
                // .. visibility
                d(
                    this.WhenAnyValue(_ => _.DetailColumn.Width)
                        .Select(_ => _.Value > 5)
                        .DistinctUntilChanged()
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
                d(this.WhenAny(_ => _.Detail.DetailEntity, _ => _.Value != null)
                    .DistinctUntilChanged()
                    .BindTo(this, _ => _.ShowDetailButton.IsEnabled));
                // Detail splitter visibility
                d(
                    this.WhenAny(_ => _.ViewModel.SelectedPlaylistItem, _ => _.Value != null)
                        .DistinctUntilChanged()
                        .BindTo(this, _ => _.DetailSplitter.IsEnabled));

                // save column settings
                d(
                    this.WhenAnyObservable(_ => _.ViewModel.DeactivateCommand)
                        .Subscribe(_ => ColumnGridBehavior.SaveSettings()));
            });
        }

        // TODO extension method
        private void BringItemIntoView(int index)
        {
            var vsp =
                (VirtualizingStackPanel)
                    typeof(ItemsControl).InvokeMember("_itemsHost",
                        BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic, null, PlaylistItems,
                        null);

            var scrollHeight = vsp.ScrollOwner.ScrollableHeight;

            // itemIndex_ is index of the item which we want to show in the middle of the view
            var offset = scrollHeight * index / PlaylistItems.Items.Count;

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
            get => ViewModel;
            set => ViewModel = (PlaylistViewModel) value;
        }

        public PlaylistViewModel ViewModel
        {
            get => (PlaylistViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public IPlayableViewModel PlayableViewModel => ViewModel;
    }
}