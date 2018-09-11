using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MoreLinq;
using PodBoy.Event;
using PodBoy.Notification;
using PodBoy.Playlists;
using PodBoy.ViewModel;
using Reactive.EventAggregator;
using ReactiveUI;
using Splat;

namespace PodBoy
{
    public class ShellViewModel : ReactiveObject, IScreen
    {
        private readonly ObservableAsPropertyHelper<bool> isBusy;

        public ShellViewModel(NotificationViewModel notificationViewModel,
            IEventAggregator eventAggregator,
            RoutingState router)
        {
            NotificationViewModel = notificationViewModel;
            EventAggregator = eventAggregator;

            // Routing
            Router = router;

            // GoBack command
            //var canGoBack = this.WhenAnyValue(vm => vm.Router.NavigationStack.Count).Select(count => count > 0);

            NavigateToPodcasts = ReactiveCommand.CreateFromTask(
                async _ => await NavigateToAsync<PodcastViewModel>(), CreateCanNavigateObservable<PodcastViewModel>());

            NavigateToPlaylist = ReactiveCommand.CreateFromTask(
                async _ => await NavigateToAsync<PlaylistViewModel>(), CreateCanNavigateObservable<PlaylistViewModel>());

            NavigateToDefaultCommand = NavigateToPodcasts;

            Router.Navigate.IsExecuting.ToProperty(this, _ => _.IsBusy, out isBusy);
        }

        public IEventAggregator EventAggregator { get; }
        public ReactiveCommand<Unit,IRoutableViewModel> NavigateToDefaultCommand { get; set; }

        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToPodcasts { get; }
        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToPlaylist { get; }
        public NotificationViewModel NotificationViewModel { get; }
        public bool IsBusy => isBusy.Value;

        public ReleaseType ReleaseType
        {
            get
            {
                if (Version.Major > 0)
                {
                    return ReleaseType.ReleaseCandidate;
                }
                if (Version.MinorRevision > 2)
                {
                    return ReleaseType.Beta;
                }
                return Version.MinorRevision > 1 ? ReleaseType.Alpha : ReleaseType.PreAlpha;
            }
        }

        public RoutingState Router { get; }
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public void NavigateBack(object _) => Router.NavigateBack.Execute();

        public void NavigateTo<T>() where T : IRoutableViewModel
            => Router.Navigate.Execute(Locator.CurrentMutable.GetService<T>());

        public IObservable<IRoutableViewModel> NavigateToAsync<T>() where T : IRoutableViewModel
            => Router.Navigate.Execute(Locator.CurrentMutable.GetService<T>());

        public void RaiseKeyboardEvent(ShortcutCommandType commandType)
        {
            EventAggregator.Publish(new ShortcutEvent(commandType));
        }

        public void DeactivateViewModels()
        {
            Router.NavigationStack.OfType<IDeactivatable>().ForEach(_ => _.Deactivate());
        }

        public async Task StartAsync()
        {
            await NavigateToDefaultCommand.Execute();
        }

        private IObservable<bool> CreateCanNavigateObservable<T>() where T : IRoutableViewModel
        {
            return
                this.WhenAnyObservable(vm => vm.Router.CurrentViewModel)
                    .Select(c => c == null || !Equals(c.UrlPathSegment, typeof(T).FullName));
        }
    }
}