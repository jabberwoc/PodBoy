using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;

namespace PodBoy.Virtualization
{
    /// <summary>
    /// Represents a provider of collection details.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public interface IItemsProvider<T>
    {
        /// <summary>
        /// Fetches the total number of items available.
        /// </summary>
        /// <returns></returns>
        int FetchCount();

        /// <summary>
        /// Fetches the total number of items available asynchronously.
        /// </summary>
        /// <returns></returns>
        Task<int> FetchCountAsync();

        /// <summary>
        /// Fetches a range of items.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        List<T> FetchRange(int startIndex, int count);

        /// <summary>
        /// Fetches a range of items asynchronously.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        Task<List<T>> FetchRangeAsync(int startIndex, int count);

        /// <summary>
        /// Observable indicating items have changed.
        /// </summary>
        IObservable<Unit> Changed { get; }

        /// <summary>
        /// Determines the index of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The index.</returns>
        int IndexOf(T item);

        T Find(int id);

        Task<T> FindAsync(int id);
    }
}