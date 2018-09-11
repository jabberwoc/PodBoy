using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using PodBoy.Entity;
using PodBoy.Extension;
using Splat;

namespace PodBoy.Feed
{
    public class FeedParser : IFeedParser, IEnableLogger
    {
        private const string LengthNoneTarget = "/rss/channel/item/enclosure[@length = \"None\"]";
        private static readonly Action<XElement> LengthNoneAction = e => e.SetAttributeValue("length", default(int));

        public readonly IDictionary<string, Action<XElement>> invalidExpressions =
            new Dictionary<string, Action<XElement>>();

        private readonly IList<RssCorrectionRule> quirkModeRules;
        private string userAgentHeader;

        private string UserAgentHeader
        {
            get { return userAgentHeader ?? (userAgentHeader = CreateUserAgentHeader()); }
        }

        private string CreateUserAgentHeader()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            return $"{assemblyName.Name}/{assemblyName.Version}";
        }

        public FeedParser()
        {
            invalidExpressions.Add(new KeyValuePair<string, Action<XElement>>(LengthNoneTarget, LengthNoneAction));
            quirkModeRules = RssCorrectionRule.AllRules();
        }

        public ItunesFeed LoadFeed(string url)
        {
            try
            {
                if (new Uri(url).IsFile)
                {
                    // read from file
                    using (var reader = XmlReader.Create(url))
                    {
                        return LoadFeedFromXml(reader, url);
                    }
                }

                // read from url
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.UserAgent = UserAgentHeader;

                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        Debug.Assert(stream != null, "stream != null");
                        using (var reader = XmlReader.Create(stream))
                        {
                            return LoadFeedFromXml(reader, url);
                        }
                    }
                }
            }
            catch (WebException e)
            {
                this.Log().Error("Error fetching feed: {0}", e.Response);
                return null;
            }
            catch (Exception e)
            {
                this.Log().Error("Error loading feed: {0}", e);
                return null;
            }
        }

        private ItunesFeed LoadFeedFromXml(XmlReader reader, string url)
        {
            try
            {
                return SyndicationFeed.Load<ItunesFeed>(reader);
            }
            catch (XmlException)
            {
                var feed = TryLoadInQuirksMode(reader, url);
                if (feed == null)
                {
                    throw;
                }
                return feed;
            }
        }

        private ItunesFeed TryLoadInQuirksMode(XmlReader reader, string uri)
        {
            var document = XDocument.Load(uri);
            var isModified = CorrectInvalidValues(document);

            if (isModified)
            {
                reader.Dispose();
                reader = document.CreateReader();

                // try again
                return SyndicationFeed.Load<ItunesFeed>(reader);
            }
            return null;
        }

        private bool CorrectInvalidValues(XDocument document)
        {
            bool isModified = false;
            foreach (var rule in quirkModeRules)
            {
                foreach (var element in document.XPathSelectElements(rule.Expression))
                {
                    if (element == null)
                    {
                        continue;
                    }
                    foreach (var correct in rule.Actions)
                    {
                        correct(element);
                    }
                    isModified = true;
                }
            }
            return isModified;
        }

        public IList<Episode> ParseItems(IEnumerable<ItunesItem> elements, Channel channel)
        {
            return elements.Where(item => !channel.HasEpisode(item)).Select(feedItem => new Episode
            {
                Channel = channel,
                Guid = feedItem.Id,
                Title = feedItem.Title.Text,
                Link = feedItem.Links.FirstOrDefault()?.Uri.AbsoluteUri,
                Date = feedItem.PublishDate.LocalDateTime,
                Description = feedItem.Description,
                Media = TryParseMedia(feedItem),
                ImageUrl = feedItem.ImageUrl ?? channel.ImageUrl
            }).ToList();
        }

        public IList<Episode> ParseItems(IEnumerable<ItunesItem> elements, ChannelInfo channelInfo)
        {
            return
                elements.Where(item => !channelInfo.EpisodeInfos.Any(_ => _.IsEquivalentTo(item)))
                    .Select(feedItem => new Episode
                    {
                        ChannelId = channelInfo.Id,
                        Guid = feedItem.Id,
                        Title = feedItem.Title.Text,
                        Link = feedItem.Links.FirstOrDefault()?.Uri.AbsoluteUri,
                        Date = feedItem.PublishDate.LocalDateTime,
                        Description = feedItem.Description,
                        Media = TryParseMedia(feedItem),
                        ImageUrl = feedItem.ImageUrl ?? channelInfo.ImageUrl
                    }).ToList();
        }

        private Media TryParseMedia(SyndicationItem mediaItem)
        {
            var enclosure = mediaItem.Links.FirstOrDefault(x => x.RelationshipType == "enclosure");
            if (enclosure == null)
            {
                // TODO
                return new Media();
            }

            return new Media(enclosure.Uri.AbsoluteUri, enclosure.MediaType, enclosure.Length);
        }

        public Channel ParseChannelInfo(ItunesFeed feed, string channelLink = null)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }

            if (channelLink == null)
            {
                channelLink = feed.GetLink();
            }

            var channel = new Channel(channelLink)
            {
                Title = feed.Title.Text,
                LastUpdated = feed.LastUpdatedTime.LocalDateTime,
                Description = feed.Description.Text,
                ImageUrl = feed.ImageUrl?.AbsoluteUri
            };
            return channel;
        }
    }
}