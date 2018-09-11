using System;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;

namespace PodBoy.Extension
{
    public static class EventExtensions
    {
        public static IObservable<EventPattern<NotifyCollectionChangedEventArgs>> CollectionChangedObservable(
            this INotifyCollectionChanged source)
        {
            return
                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => source.CollectionChanged += h, h => source.CollectionChanged -= h);
        }
    }
}