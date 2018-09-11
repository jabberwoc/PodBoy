using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Extension;
using PodBoy.Playlists;

namespace Tests
{
    public static class TestHelper
    {
        public static void InitEntities<T>(PodBoyContext context, List<T> collection = null) where T : class, IEntity
        {
            if (collection == null)
            {
                collection = new List<T>();
            }

            // channels
            var observable = new ObservableCollection<T>(collection);
            var queryable = collection.AsQueryable();
            var entitySet = Substitute.For<DbSet<T>, IQueryable<T>, IDbAsyncEnumerable<T>>();
            ((IQueryable<T>) entitySet).Provider.Returns(new TestDbAsyncQueryProvider<T>(queryable.Provider));
            ((IQueryable<T>) entitySet).Expression.Returns(queryable.Expression);
            ((IQueryable<T>) entitySet).ElementType.Returns(queryable.ElementType);
            ((IQueryable<T>) entitySet).GetEnumerator().Returns(_ => queryable.GetEnumerator());
            ((IDbAsyncEnumerable<T>) entitySet).GetAsyncEnumerator()
                .Returns(_ => new TestDbAsyncEnumerator<T>(queryable.GetEnumerator()));

            // add
            entitySet.When(_ => _.Add(Arg.Any<T>())).Do(c =>
            {
                collection.Add(c.Arg<T>());
                observable.Add(c.Arg<T>());
            });

            // add range
            entitySet.When(_ => _.AddRange(Arg.Any<IEnumerable<T>>())).Do(c =>
            {
                collection.AddRange(c.Arg<IEnumerable<T>>());
                observable.AddRange(c.Arg<IEnumerable<T>>());
            });

            // remove
            entitySet.When(_ => _.Remove(Arg.Any<T>())).Do(c =>
            {
                collection.Remove(c.Arg<T>());
                observable.Remove(c.Arg<T>());
            });

            entitySet.Find(Arg.Any<object[]>())
                .Returns(c => collection.FirstOrDefault(_ => _.Id == c.Arg<object[]>().Cast<int>().First()));
            entitySet.FindAsync(Arg.Any<object[]>())
                .Returns(
                    c => Task.FromResult(collection.FirstOrDefault(_ => _.Id == c.Arg<object[]>().Cast<int>().First())));

            context.Set<T>().Returns(entitySet);
            context.Set<T>().Local.Returns(observable);
            context.Set<T>().Include(Arg.Any<string>()).Returns(entitySet);
        }

        public static PodBoyContext CreateContext(bool initEntities = true)
        {
            var dbContext = Substitute.For<PodBoyContext>();
            if (initEntities)
            {
                InitEntities<Channel>(dbContext);
                InitEntities<Episode>(dbContext);
                InitEntities<Playlist>(dbContext);
                InitEntities<PlaylistItem>(dbContext);
            }

            return dbContext;
        }
    }
}