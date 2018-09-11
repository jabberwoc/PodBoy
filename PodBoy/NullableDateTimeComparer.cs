using System;
using System.Collections.Generic;
using PodBoy.Extension;

namespace PodBoy
{
    public class NullableDateTimeComparer : IComparer<DateTime?>
    {
        public int Compare(DateTime? x, DateTime? y)
        {
            if (!x.HasValue && !y.HasValue)
            {
                return 0;
            }

            if (!x.HasValue)
            {
                return -1;
            }

            if (!y.HasValue)
            {
                return 1;
            }

            return x.Value.CompareToReverse(y.Value);
        }
    }
}