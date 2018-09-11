using System.Reactive;
using PodBoy.Playlists;
using ReactiveUI;

namespace PodBoy
{
    public interface IPlayerModel : IReactiveObject, IDeactivatable
    {
        bool IsPlaying { get; }

        ReactiveCommand<Unit, Unit> PlayPrevious { get; }
        ReactiveCommand<Unit, Unit> PlayNext { get; }
        ReactiveCommand<Unit, Unit> ShowPlaylist { get; }

        ReactiveCommand<Unit, Unit> LocateCurrent { get; }

        void Play();

        IPlaylist Playlist { get; set; }
    }
}