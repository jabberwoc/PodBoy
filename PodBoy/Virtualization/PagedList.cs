using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using PodBoy.Entity;
using ReactiveUI;
using Splat;

namespace PodBoy.Virtualization
{
    public class PagedList<T> : ReactiveObject, IList<T>, IList, INotifyCollectionChanged where T : class, IEntity
    {
        protected readonly Dictionary<int, IList<T>> pages = new Dictionary<int, IList<T>>();
        protected readonly Dictionary<int, DateTime> pageTouchTimes = new Dictionary<int, DateTime>();
        private int count = -1;
        private IObservable<bool> isEmptyChanged;
        private bool isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public PagedList(IItemsProvider<T> itemsProvider, int pageSize = 100, int pageTimeout = 10000)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
            PageTimeout = pageTimeout;
            ObserveItemsChanged(itemsProvider);
        }

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        // TODO weak event manager refactoring
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IList<T> CachedItems
        {
            get { return pages.Values.Where(_ => _ != null).SelectMany(_ => _).ToList(); }
        }

        public int Count
        {
            get
            {
                if (count == -1)
                {
                    LoadCount();
                }
                return count;
            }
            protected set { count = value; }
        }

        public IObservable<bool> IsEmptyChanged
        {
            get
            {
                return isEmptyChanged
                       ?? (isEmptyChanged =
                           Observable
                               .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                                   h => CollectionChanged += h, h => CollectionChanged -= h)
                               .Select(_ => !pages.Any())
                               .DistinctUntilChanged());
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
        /// </summary>
        /// <value></value>
        /// <returns>Always false.
        /// </returns>
        public bool IsFixedSize => false;

        /// <summary>
        /// Gets or sets a value indicating whether the collection is loading.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this collection is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading
        {
            get { return isLoading; }
            set { this.RaiseAndSetIfChanged(ref isLoading, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>Always true.
        /// </returns>
        public bool IsReadOnly => true;

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>Always false.
        /// </returns>
        public bool IsSynchronized => false;

        public virtual TimeSpan ItemsChangedThrottle { get; } = TimeSpan.FromSeconds(0);

        /// <summary>
        /// Gets the items provider.
        /// </summary>
        /// <value>The items provider.</value>
        public IItemsProvider<T> ItemsProvider { get; }

        /// <summary>
        /// Gets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        public int PageSize { get; }

        /// <summary>
        /// Gets the page timeout.
        /// </summary>
        /// <value>The page timeout.</value>
        public long PageTimeout { get; }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </returns>
        public object SyncRoot => this;

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </exception>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Cleans up any stale pages that have not been accessed in the period dictated by PageTimeout.
        /// </summary>
        public void CleanUpPages()
        {
            var keys = new List<int>(pageTouchTimes.Keys);
            foreach (var key in keys)
            {
                // page 0 is a special case, since WPF ItemsControl access the first item frequently
                if (key != 0 && (DateTime.UtcNow - pageTouchTimes[key]).TotalMilliseconds > PageTimeout)
                {
                    pages.Remove(key);
                    pageTouchTimes.Remove(key);
                    this.Log().Info("Removed Page: {0}", key);
                }
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            OnItemsChanged();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// Always false.
        /// </returns>
        public bool Contains(T item) // where T:IEntity
        {
            if (item == null)
            {
                return false;
            }
            return CachedItems.Contains(item) || Find(item.Id) != null;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="arrayIndex"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.
        /// -or-
        /// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        /// -or-
        /// The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// -or-
        /// Type <see cref="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ItemsProvider.FetchRange(0, Count).CopyTo(array);
        }

        /// <summary>
        /// Fetches the (global) index for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The index.</returns>
        public virtual int FetchIndexForItem(T item)
        {
            return ItemsProvider.IndexOf(item);
        }

        public T Find(int id)
        {
            var cached = CachedItems.FirstOrDefault(_ => _.Id == id);
            if (cached == null)
            {
                var item = ItemsProvider.Find(id);
                if (item == null)
                {
                    return null;
                }
                var index = FetchIndexForItem(item);
                if (index == -1)
                {
                    return null;
                }

                int pageIndex = index / PageSize;
                int pageOffset = index % PageSize;

                RequestPage(pageIndex);

                return pages[pageIndex][pageOffset];
            }
            return cached;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <remarks>
        /// This method should be avoided on large collections due to poor performance.
        /// </remarks>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value)
        {
            return Contains((T) value);
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((T) value);
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, (T) value);
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        /// <returns>
        /// Always -1.
        /// </returns>
        public int IndexOf(T item)
        {
            return -1;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        /// </exception>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </exception>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        /// </exception>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Fetches the count of itmes from the IItemsProvider.
        /// </summary>
        /// <returns></returns>
        protected Task<int> FetchCount()
        {
            return ItemsProvider.FetchCountAsync();
        }

        /// <summary>
        /// Fetches the requested page from the IItemsProvider.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        protected Task<List<T>> FetchPageAsync(int pageIndex)
        {
            return ItemsProvider.FetchRangeAsync(pageIndex * PageSize, PageSize);
        }

        /// <summary>
        /// Fetches the requested page from the IItemsProvider.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <returns></returns>
        protected List<T> FetchPage(int pageIndex)
        {
            return ItemsProvider.FetchRange(pageIndex * PageSize, PageSize);
        }

        /// <summary>
        /// Asynchronously loads the count of items.
        /// </summary>
        protected virtual void LoadCount()
        {
            Count = 0;
            IsLoading = true;

            Observable.Start(FetchCount, RxApp.MainThreadScheduler).Select(_ => _.Result).Subscribe(LoadCountCompleted);
        }

        protected void LoadPageAsync(int index)
        {
            IsLoading = true;
            Observable.Start(() => FetchPageAsync(index), RxApp.MainThreadScheduler)
                .Select(_ => _.Result)
                .Subscribe(_ => LoadPageCompleted(index, _));
        }

        protected void LoadPage(int index)
        {
            var page = FetchPage(index);
            PopulatePage(index, page);
        }

        /// <summary>
        /// Raises the <see cref="E:CollectionChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var handler = CollectionChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Populates the page within the dictionary.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="page">The page.</param>
        protected void PopulatePage(int pageIndex, IList<T> page)
        {
            this.Log().Info("Page populated: {0}", pageIndex);
            if (pages.ContainsKey(pageIndex))
            {
                pages[pageIndex] = page;
            }
        }

        /// <summary>
        /// Makes a request for the specified page, creating the necessary slots in the dictionary,
        /// and updating the page touch time.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        protected void RequestPageAsync(int pageIndex)
        {
            if (!pages.ContainsKey(pageIndex))
            {
                pages.Add(pageIndex, null);
                pageTouchTimes.Add(pageIndex, DateTime.UtcNow);
                this.Log().Info("Added page: {0}", pageIndex);
                LoadPageAsync(pageIndex);
            }
            else
            {
                pageTouchTimes[pageIndex] = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Makes a request for the specified page, creating the necessary slots in the dictionary,
        /// and updating the page touch time.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        protected void RequestPage(int pageIndex)
        {
            if (!pages.ContainsKey(pageIndex))
            {
                pages.Add(pageIndex, null);
                pageTouchTimes.Add(pageIndex, DateTime.UtcNow);
                this.Log().Info("Added page: {0}", pageIndex);
                LoadPage(pageIndex);
            }
            else
            {
                pageTouchTimes[pageIndex] = DateTime.UtcNow;
            }
        }

        protected virtual void ResetPages()
        {
            pages.Clear();
            pageTouchTimes.Clear();
        }

        /// <summary>
        /// Performed on UI-thread after LoadCountWork.
        /// </summary>
        /// <param name="result">Number of items returned.</param>
        private void LoadCountCompleted(int result)
        {
            Count = result;
            IsLoading = false;
            RaiseCollectionReset();
        }

        /// <summary>
        /// Performed on UI-thread after LoadPageWork.
        /// </summary>
        /// <param name="pageIndex">The pageIndex.</param>
        /// <param name="page">The page.</param>
        private void LoadPageCompleted(int pageIndex, IList<T> page)
        {
            PopulatePage(pageIndex, page);
            IsLoading = false;
            RaiseCollectionReset();
        }

        private void ObserveItemsChanged(IItemsProvider<T> itemsProvider)
        {
            if (ItemsChangedThrottle > TimeSpan.Zero)
            {
                itemsProvider.Changed.Throttle(ItemsChangedThrottle)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => OnItemsChanged());
            }
            else
            {
                itemsProvider.Changed.ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => OnItemsChanged());
            }
        }

        private void OnItemsChanged()
        {
            ResetPages();
            LoadCount();
            RaiseCollectionReset();
        }

        /// <summary>
        /// Fires the collection reset event.
        /// </summary>
        private void RaiseCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Gets the item at the specified index. This property will fetch
        /// the corresponding page from the IItemsProvider if required.
        /// </summary>
        /// <value></value>
        public T this[int index]
        {
            get
            {
                // determine which page and offset within page
                int pageIndex = index / PageSize;
                int pageOffset = index % PageSize;

                // request primary page
                RequestPageAsync(pageIndex);

                // if accessing upper 50% then request next page
                if (pageOffset > PageSize / 2 && pageIndex < Count / PageSize)
                {
                    RequestPageAsync(pageIndex + 1);
                }

                // if accessing lower 50% then request prev page
                if (pageOffset < PageSize / 2 && pageIndex > 0)
                {
                    RequestPageAsync(pageIndex - 1);
                }

                // remove stale pages
                CleanUpPages();

                // defensive check in case of async load
                if (pages[pageIndex] == null)
                {
                    return default(T);
                }

                // return requested item
                return pages[pageIndex][pageOffset];
            }
            set { throw new NotSupportedException(); }
        }
    }
}