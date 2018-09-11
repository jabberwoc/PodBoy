using System.Diagnostics;
using Splat;

namespace PodBoy
{
    public class DefaultLogger : ILogger
    {
        public virtual void Write(string message, LogLevel logLevel)
        {
            if ((int) logLevel < (int) Level)
            {
                return;
            }

            Debug.WriteLine(message);
        }

        public LogLevel Level { get; set; }
    }
}