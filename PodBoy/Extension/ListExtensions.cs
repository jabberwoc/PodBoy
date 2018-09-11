using System.Collections.Generic;

namespace PodBoy.Extension
{
    public static class ListExtensions
    {
        /// <summary>
        /// Adds an element to a list if it is not already present.
        /// </summary>
        /// <typeparam name="T">Type of the item to be added.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="itemToAdd">The item to be added.</param>
        /// <returns><c>true</c> if the item was added, otherwise <c>false</c></returns>
        public static bool AddIfMissing<T>(this IList<T> list, T itemToAdd)
        {
            if (itemToAdd == null)
            {
                return false;
            }

            var added = false;

            if (!list.Contains(itemToAdd))
            {
                list.Add(itemToAdd);
                added = true;
            }

            return added;
        }
    }
}