using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using PodBoy;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Extension;
using PodBoy.Virtualization;
using ReactiveUI;
using Splat;
using Xunit;

namespace Tests.Virtualization
{
    public class PagedListTests
    {
        public PagedListTests()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            RxApp.TaskpoolScheduler = Scheduler.CurrentThread;

            Locator.CurrentMutable.RegisterConstant(new DefaultLogger(), typeof(ILogger));

            DbContext = TestHelper.CreateContext(false);
            TestHelper.InitEntities<TestEntity>(DbContext);
        }

        public PodBoyContext DbContext { get; }

        // TODO items are sorted / filtered

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void CollectionIsVirtualized(int pageSize)
        {
            var items = new List<TestEntity>
            {
                new TestEntity(1),
                new TestEntity(2)
            };

            DbContext.Set<TestEntity>().AddRange(items);

            // ReSharper disable once CollectionNeverUpdated.Local
            var collection = new TestPagedList(new TestRepoProvider(DbContext), pageSize);
            collection.Pages.Should().BeEmpty();

            var fecthedItems = new List<TestEntity>();

            foreach (var item in items)
            {
                // fetch element
                var element = collection[items.IndexOf(item)];

                element.Should().Be(item);
                fecthedItems.Add(element);

                // number of pages depends on no of items fetched and the pageSize
                var expectedPagesCount = Math.Max(fecthedItems.Count / pageSize, 1);

                collection.Pages.Count.Should().Be(expectedPagesCount);
                collection.Pages[expectedPagesCount - 1].Should().Contain(element);
                collection.PageTouchTimes.Count.Should().Be(expectedPagesCount);
            }
        }

        [Fact]
        public void ResetsOnProviderCollectionChanged()
        {
            var provider = new TestRepoProvider(DbContext);

            // ReSharper disable once CollectionNeverUpdated.Local
            var collection = new TestPagedList(provider, 1);

            bool wasChanged = false;
            collection.CollectionChangedObservable().Subscribe(_ => wasChanged = true);

            collection.WasResetPagesCalled.Should().BeFalse();
            collection.WasLoadCountCalled.Should().BeFalse();

            provider.SignalReset();

            collection.WasResetPagesCalled.Should().BeTrue();
            collection.WasLoadCountCalled.Should().BeTrue();
            wasChanged.Should().BeTrue();
        }
    }

    public class TestPagedList : PagedList<TestEntity>
    {
        public TestPagedList(IItemsProvider<TestEntity> itemsProvider)
            : base(itemsProvider) {}

        public TestPagedList(IItemsProvider<TestEntity> itemsProvider, int pageSize)
            : base(itemsProvider, pageSize) {}

        public TestPagedList(IItemsProvider<TestEntity> itemsProvider, int pageSize, int pageTimeout)
            : base(itemsProvider, pageSize, pageTimeout) {}

        public override TimeSpan ItemsChangedThrottle => TimeSpan.Zero;

        public Dictionary<int, IList<TestEntity>> Pages => pages;
        public Dictionary<int, DateTime> PageTouchTimes => pageTouchTimes;

        protected override void ResetPages()
        {
            base.ResetPages();
            WasResetPagesCalled = true;
        }

        protected override void LoadCount()
        {
            base.LoadCount();
            WasLoadCountCalled = true;
        }

        public bool WasLoadCountCalled { get; set; }

        public bool WasResetPagesCalled { get; set; }

        //ResetPages();
        //LoadCount();
    }

    public class TestRepoProvider : RepoProvider<TestEntity>
    {
        public TestRepoProvider(PodBoyContext context)
            : base(null, null, Observable.Return(new IOrderByExpression<TestEntity>[]
            {
                new OrderByExpression<TestEntity, int>(_ => _.Id)
            }))
        {
            CreateRepository = () => Substitute.For<PodboyRepository>(context);
        }

        public void SignalReset()
        {
            OnReset();
        }

        protected override Func<IPodboyRepository> CreateRepository { get; }
    }

    public class TestEntity : IEntity
    {
        public TestEntity(int number)
        {
            Number = number;
        }

        public int Number { get; }
        public int Id { get; set; }
    }
}