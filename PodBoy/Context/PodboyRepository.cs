using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PodBoy.Entity;
using PodBoy.Playlists;
using ReactiveUI;
using System.Data.Entity;
using System.Reactive.Subjects;
using LinqKit;
using MoreLinq;

namespace PodBoy.Context
{
    public class PodboyRepository : ReactiveObject, IPodboyRepository
    {
        private readonly PodBoyContext context;

        private static readonly Dictionary<Type, EntityType> EntityTypes = InitEntityTypes();

        public static IObservable<EntityType> SignalReset => ResetSubject;

        public PodboyRepository(PodBoyContext context = null)
        {
            this.context = context ?? new PodBoyContext();
        }

        public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return context.Set<T>().Where(predicate);
        }

        public IQueryable<TResult> Select<T, TResult>(Expression<Func<T, TResult>> selector) where T : class
        {
            return context.Set<T>().Select(selector);
        }

        public int Count<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return context.Set<T>().Count(predicate);
        }

        public async Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await context.Set<T>().CountAsync(predicate);
        }

        public IQueryable<T> Include<T>(Expression<Func<T, object>> paths) where T : class
        {
            return context.Set<T>().Include(paths);
        }

        public async Task<int> AddAndSaveOrdered<T>(T entity,
            Expression<Func<T, int>> selector,
            Expression<Func<T, bool>> filter = null) where T : class, IEntity, IOrderedEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            context.Set<T>().Add(entity);

            IQueryable<T> query = context.Set<T>();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            var maxOrderNumber = query.Select(selector).DefaultIfEmpty(0).Max();
            entity.OrderNumber = ++maxOrderNumber;

            var result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public async Task<int> AddAndSave<T>(T entity) where T : class, IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            context.Set<T>().Add(entity);

            var result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public int Max<T>(Expression<Func<T, int>> selector, Expression<Func<T, bool>> filter = null)
            where T : class, IEntity
        {
            IQueryable<T> query = context.Set<T>();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return query.Select(selector).DefaultIfEmpty(0).Max();
        }

        public async Task<int> AddRangeAndSave<T>(IEnumerable<T> entities) where T : class
        {
            AddRange(entities);
            int result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public IEnumerable<T> AddRange<T>(IEnumerable<T> entities) where T : class
        {
            return context.Set<T>().AddRange(entities);
        }

        public async Task<int> DeleteAndSave<T>(T entity) where T : class, IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (context.IsDetached(entity))
            {
                context.Set<T>().Attach(entity);
            }
            context.Set<T>().Remove(entity);

            int result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public async Task<int> DeleteRangeAndSave<T>(IEnumerable<int> entityIds) where T : class, IEntity
        {
            if (entityIds == null)
            {
                throw new ArgumentNullException(nameof(entityIds));
            }

            var entities = context.Set<T>().Where(_ => entityIds.Contains(_.Id));

            context.Set<T>().RemoveRange(entities);

            int result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public async Task<int> DeleteAndSaveOrdered<T>(T entity,
            Expression<Func<T, int>> selector,
            Expression<Func<T, bool>> filter = null) where T : class, IEntity, IOrderedEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (context.IsDetached(entity))
            {
                context.Set<T>().Attach(entity);
            }

            context.Set<T>().Remove(entity);
            IQueryable<T> query = context.Set<T>();
            if (filter != null)
            {
                query = query.Where(filter);
            }

            var orderedEntities = query.Where(_ => _.Id != entity.Id).OrderBy(selector);

            var i = 0;
            foreach (var entityToUpdate in orderedEntities)
            {
                entityToUpdate.OrderNumber = ++i;
            }

            var result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public void DeleteRangeOrdered<T>(List<int> entityIds,
            Expression<Func<T, int>> selector,
            Expression<Func<T, bool>> filter = null) where T : class, IEntity, IOrderedEntity, new()
        {
            if (entityIds == null)
            {
                throw new ArgumentNullException(nameof(entityIds));
            }

            foreach (var id in entityIds)
            {
                var entity = new T
                {
                    Id = id
                };
                context.Set<T>().Attach(entity);
                context.Set<T>().Remove(entity);
            }

            IQueryable<T> query = context.Set<T>();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            var orderedEntities = query.Where(_ => !entityIds.Contains(_.Id)).OrderBy(selector);

            var i = 0;
            foreach (var entityToUpdate in orderedEntities)
            {
                entityToUpdate.OrderNumber = ++i;
            }
        }

        public IQueryable<T> All<T>() where T : class, IEntity
        {
            foreach (var entity in context.Set<T>())
            {
                context.Detach(entity);
            }
            context.Set<T>().Load();

            return context.Set<T>();
        }

        public async Task<int> SaveAsync()
        {
            return await context.SaveChangesAsync();
        }

        public int Save()
        {
            return context.SaveChanges();
        }

        private static Dictionary<Type, EntityType> InitEntityTypes()
        {
            return new Dictionary<Type, EntityType>
            {
                {
                    typeof(Channel), EntityType.Channel
                },
                {
                    typeof(Episode), EntityType.Episode
                },
                {
                    typeof(Playlist), EntityType.Playlist
                },
                {
                    typeof(PlaylistItem), EntityType.PlaylistItem
                }
            };
        }

        public static EntityType GetEntityType(params Type[] types)
        {
            EntityType result = EntityType.None;

            foreach (var type in types)
            {
                if (result == EntityType.None)
                {
                    if (EntityTypes.ContainsKey(type))
                    {
                        result = EntityTypes[type];
                    }
                    continue;
                }
                result |= EntityTypes[type];
            }

            return result;
        }

        public static EntityType GetEntityType<T>()
        {
            return GetEntityType(typeof(T));
        }

        public async Task<int> AddChannel(Channel channel)
        {
            return await AddAndSave(channel);
        }

        public T Find<T>(int id) where T : class, IEntity
        {
            return context.Set<T>().Find(id);
        }

        public Task<T> FindAsync<T>(int id) where T : class, IEntity
        {
            return context.Set<T>().FindAsync(id);
        }

        public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity
        {
            return context.Set<T>().AnyAsync(predicate);
        }

        public async Task<int> UpdateAsyncAndSave<T>(T entity, bool signalReset = true) where T : class, IEntity
        {
            await UpdateAsync(entity);
            var result = await context.SaveChangesAsync();
            if (signalReset)
            {
                ResetSubject.OnNext(GetEntityType(typeof(T)));
            }
            return result;
        }

        public virtual async Task UpdateAsync<T>(T entity) where T : class, IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            var oldEntity = await FindAsync<T>(entity.Id);
            if (oldEntity == null)
            {
                return;
            }

            context.Entry(oldEntity).CurrentValues.SetValues(entity);
        }

        public void Update<T>(T entity) where T : class, IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            var oldEntity = Find<T>(entity.Id);
            if (oldEntity == null)
            {
                return;
            }

            context.Entry(oldEntity).CurrentValues.SetValues(entity);
        }

        private bool IsRelatedCollectionLoaded<T, TElement>(T entity, Expression<Func<T, ICollection<TElement>>> related)
            where T : class, IEntity where TElement : class
        {
            return context.Entry(entity).Collection(related).IsLoaded;
        }

        private bool IsRelatedReferenceLoaded<T, TElement>(T entity, Expression<Func<T, TElement>> related)
            where T : class, IEntity where TElement : class
        {
            return context.Entry(entity).Reference(related).IsLoaded;
        }

        public virtual async Task LoadRelatedCollection<T, TElement>(T entity,
            Expression<Func<T, ICollection<TElement>>> related,
            Expression<Func<TElement, object>> select = null) where T : class, IEntity where TElement : class
        {
            if (context.IsDetached(entity))
            {
                context.Set<T>().Attach(entity);
            }

            if (IsRelatedCollectionLoaded(entity, related))
            {
                return;
            }

            if (select == null)
            {
                await context.Entry(entity).Collection(related).LoadAsync();
            }
            else
            {
                await context.Entry(entity).Collection(related).Query().Include(select).LoadAsync();
            }
        }

        public void Attach<T>(T entity) where T : class, IEntity
        {
            if (context.IsDetached(entity))
            {
                context.Set<T>().Attach(entity);
            }
        }

        public async Task<int> DeleteAndSavePlaylistItem(PlaylistItem item)
        {
            var result = await DeleteAndSaveOrdered(item, _ => _.OrderNumber, _ => _.PlaylistId == item.PlaylistId);
            ResetSubject.OnNext(EntityType.Playlist);
            return result;
        }

        public virtual async Task LoadRelatedReference<T, TProperty>(T entity,
            Expression<Func<T, TProperty>> related,
            Expression<Func<TProperty, object>> select = null) where T : class, IEntity where TProperty : class
        {
            if (context.IsDetached(entity))
            {
                context.Set<T>().Attach(entity);
            }

            if (IsRelatedReferenceLoaded(entity, related))
            {
                return;
            }

            if (select == null)
            {
                await context.Entry(entity).Reference(related).LoadAsync();
            }
            else
            {
                await context.Entry(entity).Reference(related).Query().Include(select).LoadAsync();
            }
        }

        public async Task<int> ChangeOrderAsync<T>(T target, int newOrderNumber, Expression<Func<T, bool>> filter = null)
            where T : class, IOrderedEntity
        {
            if (context.IsDetached(target))
            {
                context.Set<T>().Attach(target);
            }

            if (filter == null)
            {
                filter = PredicateBuilder.New<T>(true);
            }

            var maxOrderNumber = Max(_ => _.OrderNumber, filter);
            var upperBound = Math.Min(maxOrderNumber, newOrderNumber);
            newOrderNumber = Math.Max(upperBound, 1);

            if (target.OrderNumber == newOrderNumber)
            {
                return 0;
            }

            IEnumerable<T> itemsToMove;
            if (newOrderNumber > target.OrderNumber)
            {
                itemsToMove =
                    context.Set<T>()
                        .AsExpandable()
                        .Where(filter.And(_ => _.OrderNumber <= newOrderNumber && _.OrderNumber > target.OrderNumber));
                itemsToMove.ForEach(_ => _.OrderNumber = --_.OrderNumber);
            }

            else
            {
                itemsToMove =
                    context.Set<T>()
                        .AsExpandable()
                        .Where(filter.And(_ => _.OrderNumber >= newOrderNumber && _.OrderNumber < target.OrderNumber));
                itemsToMove.ForEach(_ => _.OrderNumber = ++_.OrderNumber);
            }

            target.OrderNumber = newOrderNumber;
            var result = await SaveAsync();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        public int ChangeOrder<T>(T target, int newOrderNumber, Expression<Func<T, bool>> filter = null)
            where T : class, IOrderedEntity
        {
            if (context.IsDetached(target))
            {
                context.Set<T>().Attach(target);
            }

            if (filter == null)
            {
                filter = PredicateBuilder.New<T>(true);
            }

            var maxOrderNumber = Max(_ => _.OrderNumber, filter);
            var upperBound = Math.Min(maxOrderNumber, newOrderNumber);
            newOrderNumber = Math.Max(upperBound, 1);

            if (target.OrderNumber == newOrderNumber)
            {
                return 0;
            }

            IEnumerable<T> itemsToMove;
            if (newOrderNumber > target.OrderNumber)
            {
                itemsToMove =
                    context.Set<T>()
                        .AsExpandable()
                        .Where(filter.And(_ => _.OrderNumber <= newOrderNumber && _.OrderNumber > target.OrderNumber));
                itemsToMove.ForEach(_ => _.OrderNumber = --_.OrderNumber);
            }

            else
            {
                itemsToMove =
                    context.Set<T>()
                        .AsExpandable()
                        .Where(filter.And(_ => _.OrderNumber >= newOrderNumber && _.OrderNumber < target.OrderNumber));
                itemsToMove.ForEach(_ => _.OrderNumber = ++_.OrderNumber);
            }

            target.OrderNumber = newOrderNumber;
            var result = Save();
            ResetSubject.OnNext(GetEntityType(typeof(T)));
            return result;
        }

        private bool disposed;
        private static readonly Subject<EntityType> ResetSubject = new Subject<EntityType>();

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                context.Dispose();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}