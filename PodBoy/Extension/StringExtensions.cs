using System.Globalization;

namespace PodBoy.Extension
{
    public static class StringExtensions
    {
        /// <summary>
        /// String-Formatierung mit Parametern.
        /// </summary>
        /// <remarks>Siehe <see cref="string.Format(string,object)"/>.</remarks>
        /// <param name="format">Das Format.</param>
        /// <param name="args">Die Parameter.</param>
        /// <returns>Den formatierten string.</returns>
        public static string FormatString(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static bool ContainsIgnoreCase(this string source, string text)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, text, CompareOptions.IgnoreCase) >= 0;
        }
    }
}