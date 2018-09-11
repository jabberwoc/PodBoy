using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PodBoy.Playlists;

namespace PodBoy.Entity.Store
{
    public interface IPlaylistStore : IOrderedEntityStore<Playlist>
    {
        Playlist CreatePodcastList(params Episode[] episodes);

        Task<Playlist> CreatePodcastList(params int[] episodeIds);
        Task<int> AddItem(int playlistItem, Playlist playlist);
        Task<int> ChangeItemOrder(PlaylistItem source, int index);

        Task<int> ChangeItemOrderAsync(PlaylistItem item,
            int newOrderNumber,
            Expression<Func<PlaylistItem, bool>> filter = null);

        Task<int> RemoveItem(PlaylistItem item);
        Task<int> RemoveItemsByChannel(Channel channel);
        void UpdateEpisode(Episode episode);
        Task UpdateEpisodeAsync(Episode episode);
    }
}