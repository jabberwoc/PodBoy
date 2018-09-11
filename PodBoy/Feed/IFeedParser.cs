using System.Collections.Generic;
using PodBoy.Entity;

namespace PodBoy.Feed
{
    public interface IFeedParser
    {
        ItunesFeed LoadFeed(string uri);

        IList<Episode> ParseItems(IEnumerable<ItunesItem> elements, Channel channel);

        IList<Episode> ParseItems(IEnumerable<ItunesItem> elements, ChannelInfo channelInfo);

        Channel ParseChannelInfo(ItunesFeed feed, string channelLink = null);
    }
}