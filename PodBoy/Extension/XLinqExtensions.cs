using System.Xml.Linq;

namespace PodBoy.Extension
{
    public static class XLinqExtensions
    {
        /// <summary>
        /// Gets the element's value or null.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The value of the <see cref="XElement"/> or <c>null</c> if the element is null.</returns>
        public static string ValueOrNull(this XElement element)
        {
            return element?.Value;
        }
    }
}