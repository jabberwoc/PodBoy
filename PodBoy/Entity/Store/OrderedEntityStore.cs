using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Threading.Tasks;
using PodBoy.Context;

namespace PodBoy.Entity.Store
{
    public abstract class OrderedEntityStore<T> : EntityStore<T>, IOrderedEntityStore<T> where T : class, IOrderedEntity
    {
        public override async Task<int> Add(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            using (var repo = RepositoryFactory.Create())
            {
                return await Add(entity, repo);
            }
        }

        protected async Task<int> Add(T entity, IPodboyRepository repo)
        {
            var result = await repo.AddAndSaveOrdered(entity, _ => _.OrderNumber);

            if (result > 0)
            {
                Cache.Add(entity);
                resetSignal.OnNext(Unit.Default);
            }

            return result;
        }

        public override async Task<int> Remove(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            int result;
            using (var repo = RepositoryFactory.Create())
            {
                result = await repo.DeleteAndSaveOrdered(entity, _ => _.OrderNumber);
            }

            if (result > 0)
            {
                Cache.Remove(entity);
                resetSignal.OnNext(Unit.Default);
            }

            return result;
        }

        public async Task<int> ChangeOrderAsync(T target, int newOrderNumber, Expression<Func<T, bool>> filter = null)
        {
            if (!Cache.Contains(target))
            {
                Cache.Add(target);
            }

            using (var repo = RepositoryFactory.Create())
            {
                return await repo.ChangeOrderAsync(target, newOrderNumber, filter);
            }
        }
    }
}