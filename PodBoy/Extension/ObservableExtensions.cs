using System;
using System.Reactive.Linq;

namespace PodBoy.Extension
{
    public static class ObservableExtensions
    {
        public static IObservable<ValueTrack<T>> WithPrevious<T>(this IObservable<T> obs)
        {
            var next = obs.Skip(1);
            return obs.Zip(next, (old, @new) => new ValueTrack<T>(old, @new));
        }

        public static IObservable<T> Previous<T>(this IObservable<T> obs)
        {
            var next = obs.Skip(1);
            return obs.Zip(next, (old, @new) => old);
        }

        public static IObservable<T> NotNull<T>(this IObservable<T> source)
        {
            return source.Where(_ => _ != null);
        }
    }

    public class ValueTrack<T>
    {
        public ValueTrack(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; }
        public T NewValue { get; }
    }
}