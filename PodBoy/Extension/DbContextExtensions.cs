using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace PodBoy.Extension
{
    public static class DbContextExtensions
    {
        public static IQueryable<T> LocalOrDatabase<T>(this DbContext context, Expression<Func<T, bool>> expression)
            where T : class
        {
            IEnumerable<T> localResults = context.Set<T>().Local.Where(expression.Compile()).ToList();

            if (localResults.Any())
            {
                return localResults.AsQueryable();
            }

            IQueryable<T> dbResults = context.Set<T>().Where(expression);

            return dbResults;
        }
    }
}