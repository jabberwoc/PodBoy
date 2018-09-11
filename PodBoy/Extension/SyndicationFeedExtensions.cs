using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;

namespace PodBoy.Extension
{
    public static class SyndicationFeedExtensions
    {
        private const string LinkName = "link";
        private const string RssLink = "application/rss+xml";
        private static readonly XNamespace NsAtom = "http://www.w3.org/2005/Atom";

        public static string GetLink(this SyndicationFeed feed)
        {
            var targetLink = feed.Links.FirstOrDefault(_ => _.MediaType == RssLink);
            if (targetLink != null)
            {
                return targetLink.Uri.AbsoluteUri;
            }

            var targetExtension =
                feed.ElementExtensions.FirstOrDefault(
                    _ => _.OuterNamespace == NsAtom.NamespaceName && _.OuterName == LinkName);
            if (targetExtension == null)
            {
                return null;
            }
            return ParseLink(targetExtension.GetReader());
        }

        private static string ParseLink(XmlReader reader)
        {
            if (!reader.HasAttributes)
            {
                return null;
            }
            return reader.GetAttribute("href");
        }

        public static T GetExtensionElementValue<T>(this SyndicationFeed feed,
            string elementNamespace,
            string elementName)
        {
            var targetExtension =
                feed.ElementExtensions.FirstOrDefault(
                    _ => _.OuterNamespace == elementNamespace && _.OuterName == elementName);
            if (targetExtension == null)
            {
                return default(T);
            }
            return targetExtension.GetObject<T>();
        }
    }
}