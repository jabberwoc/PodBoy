using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using PodBoy.Context;
using ReactiveUI;

namespace PodBoy.Entity.Store
{
    public abstract class EntityStore<T> : IEntityStore<T> where T : class, IEntity
    {
        protected readonly Subject<Unit> resetSignal = new Subject<Unit>();

        public IObservable<Unit> ResetSignal => resetSignal;

        protected ReactiveList<T> Cache { get; } = new ReactiveList<T>
        {
            ChangeTrackingEnabled = true
        };

        public async Task<T> GetSingleAsync(int id)
        {
            var cached = Cache.FirstOrDefault(_ => _.Id == id);
            if (cached != null)
            {
                return cached;
            }

            T result;
            using (var repo = RepositoryFactory.Create())
            {
                result = await FindAsync(id, repo);
            }

            if (result != null)
            {
                AddToCache(result);
            }
            return result;
        }

        protected async Task<T> FindAsync(int id, IPodboyRepository repo)
        {
            return await repo.FindAsync<T>(id);
        }

        public virtual async Task<List<T>> GetAllAsync(Func<T, bool> predicate = null)
        {
            using (var repo = RepositoryFactory.Create())
            {
                return await GetAllAsync(repo, predicate);
            }
        }

        protected virtual async Task<List<T>> GetAllAsync(IPodboyRepository repo, Func<T, bool> predicate = null)
        {
            if (predicate == null)
            {
                predicate = _ => true;
            }

            var cachedIds = Cache.Select(_ => _.Id).ToList();

            var updates = await repo.Where<T>(_ => !cachedIds.Contains(_.Id)).ToListAsync();

            if (!updates.Any())
            {
                return Cache.Where(predicate).ToList();
            }

            AddRangeToCache(updates);

            return Cache.Where(predicate).ToList();
        }

        protected virtual void OnAddedToCache(T entity) {}

        public abstract Task LoadSingleAsync(T entity);

        public virtual async Task<int> Add(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            int result;
            using (var repo = RepositoryFactory.Create())
            {
                result = await repo.AddAndSave(entity);
            }

            if (result > 0)
            {
                AddToCache(entity);
                resetSignal.OnNext(Unit.Default);
            }

            return result;
        }

        public virtual async Task<int> Remove(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            int result;
            using (var repo = RepositoryFactory.Create())
            {
                repo.Attach(entity);
                result = await repo.DeleteAndSave(entity);
            }

            if (result > 0)
            {
                Cache.Remove(entity);
                resetSignal.OnNext(Unit.Default);
            }

            return result;
        }

        protected void AddToCache(T entity)
        {
            if (Cache.Contains(entity))
            {
                return;
            }

            Cache.Add(entity);
            OnAddedToCache(entity);
        }

        private void AddRangeToCache(List<T> entities)
        {
            entities.ForEach(_ => AddToCache(_));
        }
    }
}