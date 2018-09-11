using System;
using System.Reactive;
using PodBoy;
using PodBoy.Entity;
using PodBoy.Playlists;
using ReactiveUI;

namespace Tests
{
    public class DummyPlayerModel : ReactiveObject, IPlayerModel
    {
        private IPlaylist playlist = PodBoy.Playlists.Playlist.Empty();

        public Episode PreviousEpisode { get; set; }
        public Episode NextEpisode { get; set; }

        public IPlaylist Playlist
        {
            get => playlist;
            set => this.RaiseAndSetIfChanged(ref playlist, value);
        }

        public bool IsPlaying => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> PlayPrevious => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> PlayNext => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ShowPlaylist => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> LocateCurrent => ReactiveCommand.Create(() => { });

        public void Play()
        {
            Playlist.Current.IsPlaying = true;
        }

        public void Deactivate()
        {
            throw new NotImplementedException();
        }
    }
}