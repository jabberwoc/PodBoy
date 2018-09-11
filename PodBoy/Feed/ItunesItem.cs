using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using PodBoy.Extension;

namespace PodBoy.Feed
{
    public class ItunesItem : SyndicationItem
    {
        private readonly XNamespace nsItunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        private string description;
        private const string Prefix = "itunes";

        public string Subtitle { get; set; }
        public string Author { get; set; }

        public string Description
        {
            get { return description ?? Summary?.Text; }
            set { description = value; }
        }

        public string Duration { get; set; }
        public string Keywords { get; set; }
        public bool Explicit { get; set; }
        public string ImageUrl { get; set; }

        protected override void WriteAttributeExtensions(XmlWriter writer, string version)
        {
            writer.WriteAttributeString("xmlns", Prefix, null, nsItunes.NamespaceName);
        }

        protected override void WriteElementExtensions(XmlWriter writer, string version)
        {
            WriteItunesElement(writer, "subtitle", Subtitle);
            WriteItunesElement(writer, "author", Author);
            WriteItunesElement(writer, "summary", Summary.Text);
            WriteItunesElement(writer, "duration", Duration);
            WriteItunesElement(writer, "keywords", Keywords);
            WriteItunesElement(writer, "explicit", Explicit ? "yes" : "no");
            WriteItunesElement(writer, "image", ImageUrl);
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
                        Description = reader.ReadValue();
                        return true;
                    case "duration":
                        Duration = reader.ReadValue();
                        return true;
                    case "keywords":
                        Keywords = reader.ReadValue();
                        return true;
                    case "explicit":
                        Explicit = Equals(reader.ReadValue(), "yes");
                        return true;
                    case "image":
                        ImageUrl = reader.ReadImageUrl();
                        return true;
                    default:
                        return base.TryParseElement(reader, version);
                }
            }
            return base.TryParseElement(reader, version);
        }
    }
}