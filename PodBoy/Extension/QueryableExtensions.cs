using System;
using System.Data.Entity;
using System.Linq;
using PodBoy.Context;
using System.Linq.Expressions;

namespace PodBoy.Extension
{
    public static class QueryableExtensions
    {
        public static IQueryable<TEntity> ApplyOrderBy<TEntity>(this IQueryable<TEntity> query,
            params IOrderByExpression<TEntity>[] orderByExpressions) where TEntity : class
        {
            if (orderByExpressions == null)
            {
                return query;
            }

            IOrderedQueryable<TEntity> output = null;

            foreach (var orderByExpression in orderByExpressions)
            {
                output = output == null ? orderByExpression.ApplyOrderBy(query) : orderByExpression.ApplyThenBy(output);
            }

            return output ?? query;
        }

        public static IQueryable<TEntity> Includes<TEntity>(this IQueryable<TEntity> query,
            params Expression<Func<TEntity, object>>[] includes) where TEntity : class
        {
            if (includes == null)
            {
                return query;
            }
            return includes.Aggregate(query, (current, include) => current.Include(include));
        }
    }
}