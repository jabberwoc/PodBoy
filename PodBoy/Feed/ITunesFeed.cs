using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using PodBoy.Extension;

namespace PodBoy.Feed
{
    public class ItunesFeed : SyndicationFeed
    {
        private readonly XNamespace nsItunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        private const string Prefix = "itunes";

        public string Subtitle { get; set; }
        public string Author { get; set; }
        public string Summary { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public bool Explicit { get; set; }
        public string AtomLink { get; set; }

        public List<List<string>> itunesCategories = new List<List<string>>();

        public IEnumerable<ItunesItem> ItunesItems => Items.Cast<ItunesItem>();

        protected override void WriteAttributeExtensions(XmlWriter writer, string version)
        {
            writer.WriteAttributeString("xmlns", Prefix, null, nsItunes.NamespaceName);
        }

        protected override void WriteElementExtensions(XmlWriter writer, string version)
        {
            WriteItunesElement(writer, "subtitle", Subtitle);
            WriteItunesElement(writer, "author", Author);
            WriteItunesElement(writer, "summary", Summary);
            if (ImageUrl != null)
            {
                WriteItunesElement(writer, "image", ImageUrl.ToString());
            }
            WriteItunesElement(writer, "explicit", Explicit ? "yes" : "no");

            writer.WriteStartElement(Prefix, "owner", nsItunes.NamespaceName);
            WriteItunesElement(writer, "name", OwnerName);
            WriteItunesElement(writer, "email", OwnerEmail);
            writer.WriteEndElement();

            foreach (var category in itunesCategories)
            {
                writer.WriteStartElement(Prefix, "category", nsItunes.NamespaceName);
                writer.WriteAttributeString("text", category[0]);
                if (category.Count == 2)
                {
                    writer.WriteStartElement(Prefix, "category", nsItunes.NamespaceName);
                    writer.WriteAttributeString("text", category[1]);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        private void WriteItunesElement(XmlWriter writer, string name, string value)
        {
            if (value != null)
            {
                writer.WriteStartElement(Prefix, name, nsItunes.NamespaceName);
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
        }

        protected override bool TryParseElement(XmlReader reader, string version)
        {
            if (reader.NamespaceURI == nsItunes.NamespaceName)
            {
                switch (reader.LocalName)
                {
                    case "subtitle":
                        Subtitle = reader.ReadValue();
                        return true;
                    case "author":
                        Author = reader.ReadValue();
                        return true;
                    case "summary":
                        Summary = reader.ReadValue();
                        return true;
                    case "name":
                        OwnerName = reader.ReadValue();
                        return true;
                    case "email":
                        OwnerEmail = reader.ReadValue();
                        return true;
                    case "explicit":
                        Explicit = Equals(reader.ReadValue(), "yes");
                        return true;
                    case "image":
                        var urlString = reader.ReadImageUrl();
                        if (urlString != null)
                        {
                            ImageUrl = new Uri(urlString, UriKind.Absolute);
                        }
                        return true;
                    default:
                        return base.TryParseElement(reader, version);
                }
            }
            return base.TryParseElement(reader, version);
        }

        protected override SyndicationItem CreateItem()
        {
            return new ItunesItem();
        }
    }
}