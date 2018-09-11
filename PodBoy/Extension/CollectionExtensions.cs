using System.Collections.Generic;

namespace PodBoy.Extension
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            foreach (T item in source)
            {
                destination.Add(item);
            }
        }
    }
}