using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;

namespace PodBoy
{
    public class Player : IPlayer
    {
        private ISoundOut soundOut;
        private IWaveSource waveSource;

        private readonly Subject<PlaybackStoppedEventArgs> playbackStoppedSubject =
            new Subject<PlaybackStoppedEventArgs>();

        private double volume;
        private IDisposable soundStopped;

        public PlaybackState PlaybackState => soundOut?.PlaybackState ?? PlaybackState.Stopped;

        public TimeSpan Position
        {
            get => waveSource?.GetPosition() ?? TimeSpan.Zero;
            set => waveSource?.SetPosition(value);
        }

        public TimeSpan Length => waveSource?.GetLength() ?? TimeSpan.Zero;

        public IObservable<PlaybackStoppedEventArgs> PlaybackStopped => playbackStoppedSubject;

        public double Volume
        {
            get => volume;
            set
            {
                volume = value;

                UpdateVolume(volume);
            }
        }

        public MMDevice DefaultAudioDevice { get; private set; }

        private void UpdateVolume(double value)
        {
            if (soundOut != null)
            {
                soundOut.Volume = (float) value;
            }
        }

        public void Start()
        {
            DefaultAudioDevice = GetDefaultAudioDevice();
        }

        private static MMDevice GetDefaultAudioDevice()
        {
            using (var devices = new MMDeviceEnumerator())
            {
                return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            }
        }

        public IObservable<Unit> Open(Uri uri)
        {
            CleanupPlayback();

            return Observable.Start(() =>
            {
                var codec = CodecFactory.Instance.GetCodec(uri);
                if (codec.WaveFormat == null)
                {
                    codec.Dispose();
                    throw new NotSupportedException("Invalid format");
                }

                waveSource = codec.ToSampleSource().ToMono().ToWaveSource();

                soundOut = new WasapiOut
                {
                    Latency = 100,
                    Device = DefaultAudioDevice
                };

                soundOut.Initialize(waveSource);
                UpdateVolume(Volume);

                soundStopped =
                    Observable.FromEventPattern<PlaybackStoppedEventArgs>(ev => soundOut.Stopped += ev,
                        ev => soundOut.Stopped -= ev).Subscribe(_ => playbackStoppedSubject.OnNext(_.EventArgs));
            });
        }

        public void Play()
        {
            soundOut?.Play();
        }

        public void Pause()
        {
            soundOut?.Pause();
        }

        public void Stop()
        {
            soundOut?.Stop();
        }

        private void CleanupPlayback()
        {
            if (soundOut != null)
            {
                soundStopped?.Dispose();
                soundOut.Dispose();
                soundOut = null;
            }
            if (waveSource == null) return;

            waveSource.Dispose();
            waveSource = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            CleanupPlayback();
        }
    }
}