using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using PodBoy;
using PodBoy.Entity;
using PodBoy.Entity.Store;
using PodBoy.Extension;
using PodBoy.Feed;
using PodBoy.Playlists;
using PodBoy.ViewModel;
using Reactive.EventAggregator;
using ReactiveUI;
using Splat;
using Tests.ReactiveUI.Testing;
using Xunit;

namespace Tests.ViewModel
{
    public class PodcastViewModelTests
    {
        public PodcastViewModelTests()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            RxApp.TaskpoolScheduler = Scheduler.CurrentThread;

            Locator.CurrentMutable.RegisterConstant(new DefaultLogger(), typeof(ILogger));

            InitSubstitutes();
        }

        private void InitSubstitutes()
        {
            var feedParser = Substitute.For<IFeedParser>();
            ChannelStore = Substitute.For<IChannelStore>();
            PlaylistStore = Substitute.For<IPlaylistStore>();

            PodcastViewModel = new PodcastViewModel(Substitute.For<IScreen>(), Substitute.ForPartsOf<DummyPlayerModel>(),
                Substitute.For<IEventAggregator>(), Substitute.For<IDialogService>(), feedParser, ChannelStore,
                PlaylistStore);

            // resource service
            var resourceService = Substitute.For<IResourceService>();
            Locator.CurrentMutable.RegisterConstant(resourceService, typeof(IResourceService));
        }

        public IPlaylistStore PlaylistStore { get; set; }

        public IChannelStore ChannelStore { get; set; }

        public PodcastViewModel PodcastViewModel { get; set; }

        [Fact]
        public void StartLoadsChannelsAndRunsUpdateTimer()
        {
            new TestScheduler().With(async s =>
            {
                int updateCount = 0;
                PodcastViewModel.UpdateChannelsCommand = ReactiveCommand.CreateFromTask(_ =>
                {
                    updateCount++;
                    return Task.CompletedTask;
                });

                await PodcastViewModel.StartAsync();

                s.AdvanceByMs(1);
                updateCount.Should().Be(1, "it was called initially.");
                s.AdvanceByMs(PodcastViewModel.UpdatePeriod.TotalMilliseconds);

                updateCount.Should().Be(2, "it was called after UpdatePeriod.");
            });
        }

        // TODO Test for //StreamViewModel.ChannelList.Should().BeEquivalentTo(data);

        [Fact]
        public async Task ChannelMoveTriggersReorder()
        {
            var channel1 = new Channel("DummyLink1")
            {
                OrderNumber = 1
            };

            var channel2 = new Channel("DummyLink2")
            {
                OrderNumber = 2
            };

            var channels = new List<Channel>
            {
                channel1,
                channel2
            };

            ChannelStore.GetAllAsync().Returns(channels);
            ChannelStore.ChangeOrderAsync(Arg.Is(channel1), Arg.Is(2)).Returns(Task.FromResult(2)).AndDoes(_ =>
            {
                channel1.OrderNumber = 2;
                channel2.OrderNumber = 1;
            });

            await PodcastViewModel.InitChannels();

            // move channel1 to second position (i.e. index 1)
            PodcastViewModel.Channels.Should().HaveCount(2);
            await PodcastViewModel.MoveItemToIndex(channel1, 2);

            // channel 2 is now in first position
            PodcastViewModel.ChannelView.First().Should().Be(channel2);
            // channel 1 is now in second position
            PodcastViewModel.ChannelView.Skip(1).First().Should().Be(channel1);
        }

        [Fact]
        public async Task UpdateChannelsSavesEpisodes()
        {
            // channel
            var channel = new Channel("dummyLink");

            var newEpisode = new Episode
            {
                Title = "new episode"
            };
            var channels = new List<Channel>
            {
                channel
            };

            ChannelStore.GetAllAsync().Returns(channels);
            ChannelStore.GetChannelInfos().Returns(channels.Select(_ => new ChannelInfo
            {
                Id = _.Id,
                FeedUrl = _.Link,
                ImageUrl = _.ImageUrl,
                EpisodeInfos = _.Episodes.Select(x => new EpisodeInfo
                {
                    Id = x.Guid,
                    Date = x.Date
                })
            }));

            await PodcastViewModel.InitChannels();

            PodcastViewModel.FeedParser.LoadFeed(Arg.Any<string>()).Returns(new ItunesFeed());
            PodcastViewModel.FeedParser.ParseItems(Arg.Any<IEnumerable<ItunesItem>>(), Arg.Any<ChannelInfo>())
                .Returns(new[]
                {
                    newEpisode
                });

            PodcastViewModel.UpdateCancelToken = new CancellationTokenSource();
            await PodcastViewModel.UpdateChannelsCommand.Execute();

            // assert
            await ChannelStore.Received(1).SaveEpisodes(Arg.Is<IEnumerable<Episode>>(_ => _.Contains(newEpisode)));

            //Repository.All<Episode>().Should().Contain(newEpisode);

            //await DbContext.Received(1).SaveChangesAsync();
        }

        [Fact]
        public void PlayEpisodeSetsPlaylistAndFlags()
        {
            var episode = new Episode
            {
                Id = 1,
                IsPlayed = false,
                ChannelId = 1
            };

            PlaylistStore.CreatePodcastList(Arg.Any<Episode[]>()).Returns(new Playlist(episode.InArray()));

            PodcastViewModel.PlayItemCommand.Execute(episode);

            PodcastViewModel.Player.Playlist.Current.Item.Should().Be(episode);
            PodcastViewModel.Player.Received(1).Play();

            episode.IsPlayed.Should().BeTrue();
        }

        //[Theory]
        //[InlineData(MessageBoxResult.Yes, true, false)]
        //[InlineData(MessageBoxResult.Yes, true, true)]
        //[InlineData(MessageBoxResult.No, false, false)]
        //public async Task DeleteChannelTriggersConfirmation(MessageBoxResult result,
        //    bool wasConfirmed,
        //    bool isCancelRequested)
        //{
        //    InitChannels();
        //    StreamViewModel.ObserveChannels();

        //    var channel = new Channel("link");
        //    DbContext.Channels.Add(channel);
        //    StreamViewModel.UpdateCancelToken = new CancellationTokenSource();
        //    StreamViewModel.IsUpdating = isCancelRequested;

        //    StreamViewModel.DialogService.RequestDeleteDialog(Arg.Any<string>()).MessageBoxResult.Returns(result);

        //    await StreamViewModel.DeleteChannelCommand.ExecuteAsyncTask(channel);

        //    StreamViewModel.Channels.IsEmpty.Should().Be(wasConfirmed);

        //    StreamViewModel.UpdateCancelToken.IsCancellationRequested.Should().Be(isCancelRequested);
        //}

        [Fact]
        public async Task DeleteChannelClearsEpisodes()
        {
            var channel = new Channel("link")
            {
                Id = 1
            };

            ChannelStore.GetAllAsync().Returns(new List<Channel>
            {
                channel
            });
            ChannelStore.Remove(Arg.Is(channel)).Returns(1);

            await PodcastViewModel.InitChannels();

            PodcastViewModel.Channels.Should().NotBeEmpty();
            PodcastViewModel.EpisodeList.MonitorEvents();

            await PodcastViewModel.DeleteChannel(channel);

            PodcastViewModel.EpisodeList.ShouldRaise("CollectionChanged")
                .WithSender(PodcastViewModel.EpisodeList)
                .WithArgs<NotifyCollectionChangedEventArgs>(args => args.Action == NotifyCollectionChangedAction.Reset);

            PodcastViewModel.Channels.Should().BeEmpty("the channel was deleted");
        }

        // TODO
        [Fact]
        public async Task OpenUrlCommandLoadsChannel()
        {
            PodcastViewModel.IsUpdateEnabled = false;

            bool updateCount = false;
            PodcastViewModel.LoadChannelFromUrlCommand = ReactiveCommand.CreateFromTask<string>(_ =>
            {
                updateCount = true;
                return Task.CompletedTask;
            });

            await PodcastViewModel.OpenUrlCommand.Execute();

            updateCount.Should().BeTrue();
        }

        [Fact]
        public async Task LoadChannelFromUrlParsesFeed()
        {
            PodcastViewModel.FeedParser = new FeedParser();

            ChannelStore.Add(Arg.Any<Channel>()).Returns(1);

            var testFeedPath = Path.GetFullPath(@"..\..\Assets\feed.xml");

            await PodcastViewModel.LoadChannelFromUrlCommand.Execute(testFeedPath);

            PodcastViewModel.Channels.Should().NotBeEmpty("channel was added");

            var channel = PodcastViewModel.Channels.First();

            channel.Description.Should().NotBeEmpty();
            channel.Title.Should().NotBeEmpty();
            channel.ImageUrl.Should().NotBeEmpty();
            channel.LastUpdated.Should().Should().NotBeNull();
            channel.Link.Should().NotBeEmpty();
            channel.Episodes.Should().NotBeEmpty();
            channel.ImageUrl.Should().NotBeEmpty();

            channel.Episodes.Should().NotBeEmpty();
        }
    }
}