using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PodBoy.Entity;

namespace PodBoy.Context
{
    public interface IPodboyRepository : IDisposable
    {
        Task<int> AddAndSaveOrdered<T>(T entity,
            Expression<Func<T, int>> selector,
            Expression<Func<T, bool>> filter = null) where T : class, IEntity, IOrderedEntity;

        Task<int> AddAndSave<T>(T entity) where T : class, IEntity;
        IEnumerable<T> AddRange<T>(IEnumerable<T> entities) where T : class;

        Task<int> AddRangeAndSave<T>(IEnumerable<T> entities) where T : class;
        Task<int> DeleteAndSave<T>(T entity) where T : class, IEntity;

        Task<int> DeleteRangeAndSave<T>(IEnumerable<int> entities) where T : class, IEntity;

        Task<int> DeleteAndSaveOrdered<T>(T entity,
            Expression<Func<T, int>> selector,
            Expression<Func<T, bool>> filter = null) where T : class, IEntity, IOrderedEntity;

        void DeleteRangeOrdered<T>(List<int> entityIds,
            Expression<Func<T, int>> selector,
            Expression<Func<T, bool>> filter = null) where T : class, IEntity, IOrderedEntity, new();

        int Max<T>(Expression<Func<T, int>> selector, Expression<Func<T, bool>> filter = null) where T : class, IEntity;

        IQueryable<T> All<T>() where T : class, IEntity;

        Task<int> SaveAsync();

        int Save();

        IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class;

        IQueryable<TResult> Select<T, TResult>(Expression<Func<T, TResult>> selector) where T : class;

        int Count<T>(Expression<Func<T, bool>> predicate) where T : class;

        Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate) where T : class;

        IQueryable<T> Include<T>(Expression<Func<T, object>> path) where T : class;

        T Find<T>(int id) where T : class, IEntity;

        Task<T> FindAsync<T>(int id) where T : class, IEntity;

        Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity;

        int ChangeOrder<T>(T target, int newOrderNumber, Expression<Func<T, bool>> filter = null)
            where T : class, IOrderedEntity;

        Task<int> ChangeOrderAsync<T>(T target, int newOrderNumber, Expression<Func<T, bool>> filter = null)
            where T : class, IOrderedEntity;

        Task<int> UpdateAsyncAndSave<T>(T entity, bool signalReset = true) where T : class, IEntity;

        Task UpdateAsync<T>(T entity) where T : class, IEntity;

        void Update<T>(T entity) where T : class, IEntity;

        Task LoadRelatedReference<T, TProperty>(T entity,
            Expression<Func<T, TProperty>> related,
            Expression<Func<TProperty, object>> select = null) where T : class, IEntity where TProperty : class;

        Task LoadRelatedCollection<T, TElement>(T entity,
            Expression<Func<T, ICollection<TElement>>> related,
            Expression<Func<TElement, object>> select = null) where T : class, IEntity where TElement : class;

        void Attach<T>(T entity) where T : class, IEntity;
    }
}