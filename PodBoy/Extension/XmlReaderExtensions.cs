using System.Xml;
using System.Xml.Linq;

namespace PodBoy.Extension
{
    public static class XmlReaderExtensions
    {
        public static string ReadImageUrl(this XmlReader reader)
        {
            var imageElement = (XElement) XNode.ReadFrom(reader);

            if (!imageElement.HasAttributes || imageElement.Attribute("href") == null)
            {
                return null;
            }

            return imageElement.Attribute("href").Value;
        }

        public static string ReadValue(this XmlReader reader)
        {
            var element = (XElement) XNode.ReadFrom(reader);
            return string.IsNullOrEmpty(element.Value) ? null : element.Value;
        }
    }
}