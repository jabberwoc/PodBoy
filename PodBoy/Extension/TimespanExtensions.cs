using System;

namespace PodBoy.Extension
{
    public static class TimespanExtensions
    {
        public static string ToShortFormat(this TimeSpan timeSpan)
        {
            return timeSpan.ToString(timeSpan.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");
        }
    }
}