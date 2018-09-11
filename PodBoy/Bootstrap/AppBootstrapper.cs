using Autofac;
using PodBoy.Cache;
using PodBoy.Context;
using PodBoy.Entity.Store;
using PodBoy.Feed;
using PodBoy.Notification;
using PodBoy.Playlists;
using PodBoy.Resource.Control;
using PodBoy.View;
using PodBoy.ViewModel;
using Reactive.EventAggregator;
using ReactiveUI;
using Splat;

namespace PodBoy.Bootstrap
{
    public class AppBootstrapper
    {
        public void Run(bool showShell = true)
        {
            var builder = new ContainerBoy();

            builder.RegisterType<CommandBinderImplementation>().AsImplementedInterfaces();

            builder.RegisterType<PodcastView>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PodcastViewModel>()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance()
                .OnActivated(async _ => await _.Instance.StartAsync());

            builder.RegisterType<Player>().AsSelf().As<IPlayer>().SingleInstance().OnActivated(_ => _.Instance.Start());

            builder.RegisterType<PlayerView>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PlayerViewModel>()
                .AsSelf()
                .As<IPlayerModel>()
                .SingleInstance()
                .OnActivated(async _ => await _.Instance.StartAsync());

            builder.RegisterType<PlaylistView>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PlaylistViewModel>()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance()
                .OnActivated(_ => _.Instance.Start());

            builder.RegisterType<OpenUrlView>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OpenUrlViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<DetailViewModel>().InstancePerDependency();

            builder.RegisterType<NotificationViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ShellViewModel>()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance()
                .OnActivated(async _ => await _.Instance.StartAsync());
            builder.RegisterType<Shell>().AsSelf();

            builder.RegisterType<EventAggregator>().As<IEventAggregator>().SingleInstance();
            builder.RegisterType<PodboyRepository>().As<IPodboyRepository>().InstancePerDependency();

            builder.RegisterType<ChannelStore>().As<IChannelStore>().SingleInstance();
            builder.RegisterType<PlaylistStore>().As<IPlaylistStore>().SingleInstance();

            builder.RegisterType<ImageMemoryCache>().As<IImageCache>().SingleInstance();
            builder.RegisterType<NotificationViewModel>().SingleInstance();
            builder.RegisterType<ResourceService>().AsImplementedInterfaces();

            builder.RegisterType<DialogService>().As<IDialogService>().SingleInstance();
            builder.RegisterType<FeedParser>().As<IFeedParser>().SingleInstance();

            builder.RegisterType<RoutingState>().AsSelf().InstancePerDependency();

            builder.RegisterType<ConfirmDeleteDialog>().As<IConfirmDeleteDialog>();

            builder.InitializeSplat();
            builder.InitializeReactiveUI();

            builder.RegisterType<DebugLogger>().As<ILogger>().SingleInstance();

            var resolver = new AutofacDependencyResolver(builder.Build());
            Locator.Current = resolver;

            if (showShell)
            {
                ShowShell();
            }
        }

        public void ShowShell()
        {
            var shell = Locator.CurrentMutable.GetService<Shell>();

            shell.Show();
        }

        public void Dispose()
        {
            Locator.Current.Dispose();
        }
    }
}