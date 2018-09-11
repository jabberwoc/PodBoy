using System;

namespace PodBoy.Extension
{
    public static class DateExtensions
    {
        public static int CompareToReverse(this DateTime date, DateTime anotherDate)
        {
            long valueTicks = anotherDate.Ticks;
            long ticks = date.Ticks;
            if (ticks > valueTicks)
            {
                return -1;
            }
            return ticks < valueTicks ? 1 : 0;
        }
    }
}