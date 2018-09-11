using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Extension;

namespace PodBoy.Virtualization
{
    /// <summary>
    /// Episode provider as implementation of IItemsProvider.
    /// </summary>
    public class RepoProvider<T> : IItemsProvider<T> where T : class, IEntity
    {
        private readonly int fetchDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepoProvider{T}"/> class.
        /// </summary>
        /// <param name="includes"></param>
        /// <param name="filter"></param>
        /// <param name="orderByObservable"></param>
        /// <param name="fetchDelay">The fetch delay.</param>
        public RepoProvider(Expression<Func<T, object>>[] includes,
            ProviderFilter<T> filter,
            IObservable<IOrderByExpression<T>[]> orderByObservable,
            int fetchDelay = 0)
        {
            this.fetchDelay = fetchDelay;
            entityType = PodboyRepository.GetEntityType<T>();

            Includes = includes;

            Filter = filter?.FilterExpression ?? (_ => true);

            if (orderByObservable == null)
            {
                orderByObservable = Observable.Return(new IOrderByExpression<T>[]
                {
                    new OrderByExpression<T, int>(_ => _.Id, true)
                });
            }
            orderByObservable.Subscribe(_ => OrderBy = _);

            var typePredicate = entityType == EntityType.None
                ? new Func<EntityType, bool>(_ => false)
                : (_ => _.HasFlag(entityType));

            var resetObservable =
                PodboyRepository.SignalReset.Where(typePredicate)
                    .Select(_ => Unit.Default)
                    .Merge(orderByObservable.Select(_ => Unit.Default));

            if (filter?.SignalReset != null)
            {
                resetObservable = resetObservable.Merge(filter.SignalReset);
            }

            resetObservable.Subscribe(_ => OnReset());
        }

        public IObservable<Unit> Changed => reset;

        private readonly Subject<Unit> reset = new Subject<Unit>();
        private readonly EntityType entityType;
        protected virtual Func<IPodboyRepository> CreateRepository => RepositoryFactory.Create;

        protected Expression<Func<T, bool>> Filter { get; }

        protected IOrderByExpression<T>[] OrderBy { get; set; }

        protected Expression<Func<T, object>>[] Includes { get; }

        protected void OnReset()
        {
            reset.OnNext(Unit.Default);
        }

        /// <summary>
        /// Fetches the total number of items available.
        /// </summary>
        /// <returns></returns>
        public int FetchCount()
        {
            Debug.WriteLine("FetchCount");
            Delay();

            using (var repo = CreateRepository())
            {
                return repo.Count(Filter);
            }
        }

        public async Task<int> FetchCountAsync()
        {
            Debug.WriteLine("FetchCount");
            Delay();

            using (var repo = CreateRepository())
            {
                return await repo.CountAsync(Filter);
            }
        }

        private void Delay()
        {
            if (fetchDelay > 0)
            {
                Thread.Sleep(fetchDelay);
            }
        }

        /// <summary>
        /// Fetches a range of items.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        /// <returns></returns>
        public List<T> FetchRange(int startIndex, int count)
        {
            Delay();

            using (var repo = CreateRepository())
            {
                return repo.Where(Filter).Includes(Includes).ApplyOrderBy(OrderBy).Skip(startIndex).Take(count).ToList();
            }
        }

        /// <summary>
        /// Fetches a range of items.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        /// <returns></returns>
        public async Task<List<T>> FetchRangeAsync(int startIndex, int count)
        {
            Delay();

            using (var repo = CreateRepository())
            {
                return
                    await
                        repo.Where(Filter)
                            .Includes(Includes)
                            .ApplyOrderBy(OrderBy)
                            .Skip(startIndex)
                            .Take(count)
                            .ToListAsync();
            }
        }

        /// <summary>
        /// Determines the index of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The index.</returns>
        public int IndexOf(T item)
        {
            using (var repo = CreateRepository())
            {
                var result = repo.Where(Filter).ApplyOrderBy(OrderBy).AsEnumerable().Select((_, i) => new
                {
                    Item = _,
                    Index = i
                }).FirstOrDefault(_ => _.Item.Id == item.Id);

                var index = -1;
                if (result != null)
                {
                    index = result.Index;
                }

                return index;
            }
        }

        public T Find(int id)
        {
            using (var repo = CreateRepository())
            {
                return repo.Find<T>(id);
            }
        }

        public async Task<T> FindAsync(int id)
        {
            using (var ctx = CreateRepository())
            {
                return await ctx.FindAsync<T>(id);
            }
        }
    }
}