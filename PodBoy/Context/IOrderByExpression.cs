using System;
using System.Linq;
using System.Linq.Expressions;

namespace PodBoy.Context
{
    public interface IOrderByExpression<TEntity> where TEntity : class
    {
        IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> query);
        IOrderedQueryable<TEntity> ApplyThenBy(IOrderedQueryable<TEntity> query);
    }
}