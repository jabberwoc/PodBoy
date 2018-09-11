using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodBoy.Entity.Store
{
    public interface IChannelStore : IOrderedEntityStore<Channel>
    {
        Task MarkChannelPlayed(Channel channel);
        Task UpdateUnplayedCount(int channelID);
        Task<IEnumerable<ChannelInfo>> GetChannelInfos();

        Task UpdateEpisode(Episode episode);
        Task<int> SaveEpisodes(IEnumerable<Episode> episodes);
    }
}