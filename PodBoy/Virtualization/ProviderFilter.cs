using System;
using System.Linq.Expressions;
using System.Reactive;

namespace PodBoy.Virtualization
{
    public class ProviderFilter<T>
    {
        public ProviderFilter(Expression<Func<T, bool>> filterExpression, IObservable<Unit> signalReset)
        {
            FilterExpression = filterExpression;
            SignalReset = signalReset;
        }

        public Expression<Func<T, bool>> FilterExpression { get; }
        public IObservable<Unit> SignalReset { get; }
    }
}