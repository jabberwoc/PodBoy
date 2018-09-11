using System;
using System.Linq;
using System.Linq.Expressions;

namespace PodBoy.Context
{
    public class OrderByExpression<TEntity, TOrderBy> : IOrderByExpression<TEntity> where TEntity : class
    {
        private readonly Expression<Func<TEntity, TOrderBy>> expression;
        private readonly bool isDescending;

        public OrderByExpression(Expression<Func<TEntity, TOrderBy>> expression, bool isDescending = false)
        {
            this.expression = expression;
            this.isDescending = isDescending;
        }

        public IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> query)
        {
            return isDescending ? query.OrderByDescending(expression) : query.OrderBy(expression);
        }

        public IOrderedQueryable<TEntity> ApplyThenBy(IOrderedQueryable<TEntity> query)
        {
            return isDescending ? query.ThenByDescending(expression) : query.ThenBy(expression);
        }
    }
}