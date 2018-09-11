using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PodBoy.Playlists
{
    public interface IPlaylistManager
    {
        Task<Playlist> GetPlaylistAsync(int id);

        Task<List<Playlist>> GetPlaylistsAsync(Func<Playlist, bool> predicate = null);

        Task LoadPlaylistAsync(Playlist playlist);
        Task<Playlist> CreatePodcastList(int episodeId);
        Task Add(Playlist playlist);
        Task<int> MoveItem(Playlist source, int index);
        Task<int> AddItem(PlaylistItem playlistItem, Playlist playlist);
        Task Remove(Playlist playlist);
    }
}