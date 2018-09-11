using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using PodBoy;
using PodBoy.Entity;
using PodBoy.Entity.Store;
using PodBoy.Event;
using PodBoy.Playlists;
using PodBoy.ViewModel;
using Reactive.EventAggregator;
using ReactiveUI;
using Xunit;

namespace Tests.ViewModel
{
    public class PlayerViewModelTests
    {
        public PlayerViewModelTests()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            RxApp.TaskpoolScheduler = Scheduler.CurrentThread;
            var eventAggregator = Substitute.For<IEventAggregator>();
            var eventSubject = new Subject<ShortcutEvent>();
            ShortcutObservable = eventSubject;
            eventAggregator.GetEvent<ShortcutEvent>().Returns(eventSubject);
            PlayerViewModel = new PlayerViewModel(eventAggregator, Substitute.For<IPlaylistStore>(),
                Substitute.For<IPlayer>());
        }

        public IObserver<ShortcutEvent> ShortcutObservable { get; }

        public PlayerViewModel PlayerViewModel { get; set; }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentEpisodeSelectsEpisode(bool isPlaying)
        {
            var episode = new Episode
            {
                Link = "dummyLink",
                Title = "test",
                ElapsedSeconds = 42,
                Media = new Media("http://dummyUrl", "dummyType", 60)
            };

            PlayerViewModel.Playlist = new Playlist(new[]
            {
                episode
            });
            PlayerViewModel.IsPlaying = isPlaying;

            // set episode
            PlayerViewModel.Playlist.SetCurrent(episode);

            // episode is active
            episode.IsActive.Should().BeTrue();
            episode.IsPlaying.Should().Be(isPlaying);

            // episode media is set
            PlayerViewModel.MediaUri.Should().Be(episode.Media.Url);
            PlayerViewModel.Title.Should().Be(episode.Title);
            PlayerViewModel.StreamPosition.Should().Be(TimeSpan.FromSeconds(episode.ElapsedSeconds));
        }

        [Fact]
        public void SelectEpisodeUpdatesImageProvider()
        {
            var episode = new Episode
            {
                Media = new Media
                {
                    Url = "bla"
                },
                ImageUrl = "http://dummyUrl"
            };

            PlayerViewModel.ImageProvider.ImageUri.Should().BeNull();

            // set playlist & episode
            PlayerViewModel.Playlist = new Playlist(new[]
            {
                episode
            });
            PlayerViewModel.Playlist.SetCurrent(episode);

            // uri should be set
            PlayerViewModel.ImageProvider.ImageUri.Should().Be(episode.ImageUri);
        }

        [Fact]
        public void SelectEpisodeSetsActive()
        {
            var activeEpisode = new Episode
            {
                Title = "activeEpisode"
            };
            var currentEpisode = new Episode
            {
                Title = "currentEpisode"
            };

            // set playlist & episode
            PlayerViewModel.Playlist = new Playlist(new[]
            {
                activeEpisode,
                currentEpisode
            });
            PlayerViewModel.IsPlaying = true;
            PlayerViewModel.Playlist.SetCurrent(currentEpisode);

            activeEpisode.IsActive.Should().BeFalse();
            activeEpisode.IsPlaying.Should().BeFalse();

            currentEpisode.IsActive.Should().BeTrue();
            currentEpisode.IsPlaying.Should().BeTrue();
        }

        [Fact]
        public void IsPlayingSetsCurrentPlaying()
        {
            var episode = new Episode
            {
                IsPlaying = false
            };
            PlayerViewModel.Playlist = new Playlist(new[]
            {
                episode
            });
            PlayerViewModel.Playlist.SetCurrent(episode);

            // set IsPlaying
            PlayerViewModel.IsPlaying = true;

            episode.IsPlaying.Should().BeTrue();
        }

        [Fact]
        public void ShortcutCommandTogglesPlay()
        {
            // initial state
            PlayerViewModel.IsPlaying.Should().BeFalse();

            var togglePlayCalled = false;
            PlayerViewModel.TogglePlayCommand.Subscribe(_ => togglePlayCalled = true);

            // shortcut event
            ShortcutObservable.OnNext(new ShortcutEvent(ShortcutCommandType.TogglePlay));

            // toggled
            PlayerViewModel.IsPlaying.Should().BeTrue();

            // command executed
            togglePlayCalled.Should().BeTrue();
        }

        [Fact]
        public void PlayPreviousSelectsEpisode()
        {
            var currentEpisode = new Episode
            {
                Title = "current"
            };
            var previousEpisode = new Episode
            {
                Title = "previous"
            };
            var nextEpisode = new Episode
            {
                Title = "next"
            };

            // set playlist
            PlayerViewModel.Playlist = new Playlist(new[]
            {
                previousEpisode,
                currentEpisode,
                nextEpisode
            });

            // set current episode
            PlayerViewModel.Playlist.SetCurrent(currentEpisode);

            // play previous
            PlayerViewModel.PlayPrevious.Execute().Subscribe();

            // current should be "previous"
            PlayerViewModel.Playlist.Current.Item.Should().Be(previousEpisode);
            PlayerViewModel.Playlist.Next.Item.Should().Be(currentEpisode);
            PlayerViewModel.Playlist.Previous.Should().BeNull();

            // play next
            PlayerViewModel.PlayNext.Execute().Subscribe();

            // current should be "current" again
            PlayerViewModel.Playlist.Current.Item.Should().Be(currentEpisode);
            PlayerViewModel.Playlist.Next.Item.Should().Be(nextEpisode);
            PlayerViewModel.Playlist.Previous.Item.Should().Be(previousEpisode);
        }

        [Fact]
        public void PlayNextSelectsEpisode()
        {
            var currentEpisode = new Episode
            {
                Title = "current"
            };
            var previousEpisode = new Episode
            {
                Title = "previous"
            };
            var nextEpisode = new Episode
            {
                Title = "next"
            };

            // set playlist
            PlayerViewModel.Playlist = new Playlist(new[]
            {
                previousEpisode,
                currentEpisode,
                nextEpisode
            });

            // set current episode
            PlayerViewModel.Playlist.SetCurrent(currentEpisode);

            // play next
            PlayerViewModel.PlayNext.Execute().Subscribe();

            // current should be "next"
            PlayerViewModel.Playlist.Current.Item.Should().Be(nextEpisode);
            PlayerViewModel.Playlist.Next.Should().BeNull();
            PlayerViewModel.Playlist.Previous.Item.Should().Be(currentEpisode);

            // play previous
            PlayerViewModel.PlayPrevious.Execute().Subscribe();

            // current should be "current" again
            PlayerViewModel.Playlist.Current.Item.Should().Be(currentEpisode);
            PlayerViewModel.Playlist.Next.Item.Should().Be(nextEpisode);
            PlayerViewModel.Playlist.Previous.Item.Should().Be(previousEpisode);
        }
    }
}