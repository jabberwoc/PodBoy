using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;

namespace PodBoy.Entity.Store
{
    public interface IEntityStore<T> where T : IEntity
    {
        IObservable<Unit> ResetSignal { get; }
        Task<T> GetSingleAsync(int id);
        Task<List<T>> GetAllAsync(Func<T, bool> predicate = null);
        Task LoadSingleAsync(T entity);
        Task<int> Add(T entity);
        Task<int> Remove(T entity);
    }
}