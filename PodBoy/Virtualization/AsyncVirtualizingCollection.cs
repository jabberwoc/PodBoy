using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace PodBoy.Virtualization
{
    /// <summary>
    /// Derived VirtualizatingCollection, performing loading asychronously.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public class AsyncVirtualizingCollection<T> : VirtualizingCollection<T>, INotifyCollectionChanged,
                                                  INotifyPropertyChanged
    {
        private bool isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider)
            : base(itemsProvider)
        {
            ObserveItemsChanged(itemsProvider);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
            : base(itemsProvider, pageSize)
        {
            ObserveItemsChanged(itemsProvider);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncVirtualizingCollection{T}"/> class.
        /// </summary>
        /// <param name="itemsProvider">The items provider.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageTimeout">The page timeout.</param>
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
            : base(itemsProvider, pageSize, pageTimeout)
        {
            ObserveItemsChanged(itemsProvider);
        }

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether the collection is loading.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this collection is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (value != isLoading)
                {
                    isLoading = value;
                    RaisePropertyChanged();
                }
            }
        }

        public virtual TimeSpan ItemsChangedThrottle { get; } = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Asynchronously loads the count of items.
        /// </summary>
        protected override async void LoadCount()
        {
            Count = 0;
            IsLoading = true;

            var count = await Observable.Start(FetchCount, RxApp.TaskpoolScheduler);
            LoadCountCompleted(count);
        }

        /// <summary>
        /// Asynchronously loads the page.
        /// </summary>
        /// <param name="index">The index.</param>
        protected override async void LoadPage(int index)
        {
            IsLoading = true;
            var page = await Observable.Start(() => FetchPage(index), RxApp.MainThreadScheduler);
            //.ObserveOn(RxApp.MainThreadScheduler)
            //.Subscribe(page => LoadPageCompleted(index, page)).;

            LoadPageCompleted(index, page);
        }

        /// <summary>
        /// Raises the <see cref="E:CollectionChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var handler = CollectionChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Performed on UI-thread after LoadCountWork.
        /// </summary>
        /// <param name="count">Number of items returned.</param>
        private void LoadCountCompleted(int count)
        {
            Count = count;
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
        /// Fires the property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public T Find(int id)
        {
            return ItemsProvider.Find(id);
        }
    }
}