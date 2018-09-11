using System;
using System.Reactive;
using PodBoy.Context;
using PodBoy.Entity;

namespace PodBoy.Virtualization
{
    public class ProviderOrder<T>
    {
        public ProviderOrder(IObservable<IOrderByExpression<Episode>[]> orderObservable, IObservable<Unit> signalReset)
        {
            OrderObservable = orderObservable;
            SignalReset = signalReset;
        }

        public IObservable<IOrderByExpression<Episode>[]> OrderObservable { get; }
        public IObservable<Unit> SignalReset { get; }
    }
}