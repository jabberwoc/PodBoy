using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Extension;
using PodBoy.Playlists;
using ReactiveUI;
using Xunit;

namespace Tests.Context
{
    public class PodboyRepositoryTests
    {
        public PodboyRepositoryTests()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            RxApp.TaskpoolScheduler = Scheduler.CurrentThread;

            InitSubstitutes();
        }

        private void InitSubstitutes()
        {
            DbContext = TestHelper.CreateContext();
            Repository = Substitute.For<PodboyRepository>(DbContext);
        }

        public PodBoyContext DbContext { get; private set; }

        public PodboyRepository Repository { get; private set; }

        // TODO Signal reset check on respective test methods

        // TODO DeleteAndSave(Channel channel)

        [Fact]
        public async Task AddAndSaveOrderedChangesOrder()
        {
            var channel1 = new Channel(string.Empty)
            {
                Id = 1,
                OrderNumber = 1
            };

            var channel2 = new Channel(string.Empty);

            var channels = new List<Channel>
            {
                channel1,
                channel2
            };

            DbContext.SaveChangesAsync().Returns(Task.FromResult(1));
            DbContext.Set<Channel>().Add(channel1);
            bool wasResetSignalled = false;
            var resetObservable =
                PodboyRepository.SignalReset.Where(_ => _ == EntityType.Channel)
                    .Subscribe(_ => wasResetSignalled = true);
            using (resetObservable)
            {
                await Repository.AddAndSaveOrdered(channel2, _ => _.OrderNumber);
            }

            Repository.All<Channel>().Should().BeEquivalentTo(channels);
            channel2.OrderNumber.Should().Be(2);
            wasResetSignalled.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAndSaveOrderedChangesOrder()
        {
            var playlist1 = new Playlist
            {
                Id = 1,
                OrderNumber = 1
            };

            var playlist2 = new Playlist
            {
                Id = 2,
                OrderNumber = 2
            };

            var playlists = new List<Playlist>
            {
                playlist1,
                playlist2
            };

            DbContext.SaveChangesAsync().Returns(Task.FromResult(1));
            DbContext.Set<Playlist>().AddRange(playlists);
            bool wasResetSignalled = false;
            var resetObservable =
                PodboyRepository.SignalReset.Where(_ => _ == EntityType.Playlist)
                    .Subscribe(_ => wasResetSignalled = true);

            using (resetObservable)
            {
                await Repository.DeleteAndSaveOrdered(playlist1, _ => _.OrderNumber);
            }

            Repository.All<Playlist>().Should().ContainSingle(_ => Equals(_, playlist2));
            playlist2.OrderNumber.Should().Be(1);
            wasResetSignalled.Should().BeTrue();
        }

        [Theory]
        [InlineData(typeof(Channel), EntityType.Channel)]
        [InlineData(typeof(Episode), EntityType.Episode)]
        [InlineData(typeof(Playlist), EntityType.Playlist)]
        [InlineData(typeof(PlaylistItem), EntityType.PlaylistItem)]
        public void GetEntityTypeTest(Type entityType, EntityType expected)
        {
            entityType.Should().Implement(typeof(IEntity));

            PodboyRepository.GetEntityType(entityType.InArray()).Should().Be(expected);
        }
    }
}