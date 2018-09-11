using System.Reactive.Concurrency;
using NSubstitute;
using PodBoy;
using PodBoy.Context;
using PodBoy.Entity.Store;
using PodBoy.Playlists;
using Reactive.EventAggregator;
using ReactiveUI;
using Splat;

namespace Tests.Playlists
{
    public class PlaylistViewModelTests
    {
        public PlaylistViewModelTests()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            Locator.CurrentMutable.RegisterConstant(new DefaultLogger(), typeof(ILogger));

            InitSubstitutes();
        }

        private void InitSubstitutes()
        {
            DbContext = TestHelper.CreateContext();

            Repository = Substitute.For<PodboyRepository>(DbContext);

            PlaylistViewModel = new PlaylistViewModel(Substitute.For<IScreen>(), Substitute.For<IEventAggregator>(),
                Substitute.ForPartsOf<DummyPlayerModel>(), Substitute.For<IPlaylistStore>());
        }

        public RepositoryFactory ContextFactory { get; set; }

        public PlaylistViewModel PlaylistViewModel { get; set; }

        public PodBoyContext DbContext { get; set; }

        public IPodboyRepository Repository { get; set; }

        // TODO
    }
}