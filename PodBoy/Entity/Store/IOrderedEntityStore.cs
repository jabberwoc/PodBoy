using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PodBoy.Entity.Store
{
    public interface IOrderedEntityStore<T> : IEntityStore<T> where T : IEntity
    {
        Task<int> ChangeOrderAsync(T target, int newOrderNumber, Expression<Func<T, bool>> filter = null);
    }
}