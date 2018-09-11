using System.Collections.Generic;

namespace PodBoy
{
    public class ChannelInfo
    {
        public int Id { get; set; }
        public string FeedUrl { get; set; }

        public string ImageUrl { get; set; }
        public IEnumerable<EpisodeInfo> EpisodeInfos { get; set; }
    }
}