using System;
using System.Reactive;
using CSCore.SoundOut;

namespace PodBoy
{
    public interface IPlayer : IDisposable
    {
        IObservable<PlaybackStoppedEventArgs> PlaybackStopped { get; }
        double Volume { get; set; }
        TimeSpan Length { get; }
        TimeSpan Position { get; set; }
        void Play();
        void Pause();
        void Stop();
        IObservable<Unit> Open(Uri uri);
    }
}