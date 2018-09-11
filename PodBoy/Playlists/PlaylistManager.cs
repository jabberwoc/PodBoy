using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using PodBoy.Context;
using PodBoy.Entity;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Playlists
{
    public class PlaylistManager : IPlaylistManager
    {
        private readonly IPodboyRepository repo;

        private readonly ReactiveList<Playlist> playlistCache = new ReactiveList<Playlist>
        {
            ChangeTrackingEnabled = true
        };

        public PlaylistManager(IPodboyRepository repo)
        {
            this.repo = repo;

            playlistCache.ItemChanged.Where(_ => _.Sender.Id != 0)
                .Where(_ => _.PropertyName == nameof(Playlist.Name) || _.PropertyName == nameof(Playlist.Current))
                .Skip(1)
                .Throttle(TimeSpan.FromSeconds(0.5))
                //.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await repo.SaveAsync());
        }

        public async Task<Playlist> GetPlaylistAsync(int id)
        {
            var cached = playlistCache.FirstOrDefault(_ => _.Id == id);
            if (cached != null)
            {
                return cached;
            }

            var result = await repo.FindAsync<Playlist>(id);
            if (result != null)
            {
                playlistCache.Add(result);
            }
            return result;
        }

        public async Task<List<Playlist>> GetPlaylistsAsync(Func<Playlist, bool> predicate = null)
        {
            if (predicate == null)
            {
                predicate = _ => true;
            }
            var cachedIds = playlistCache.Select(_ => _.Id).ToList();
            var updates = await repo.Where<Playlist>(_ => !cachedIds.Contains(_.Id)).ToListAsync();
            if (!updates.Any())
            {
                return playlistCache.Where(predicate).ToList();
            }

            playlistCache.AddRange(updates);

            return playlistCache.Where(predicate).ToList();
        }

        public async Task LoadPlaylistAsync(Playlist playlist)
        {
            if (playlist.Id != 0)
            {
                await repo.LoadRelatedCollection(playlist, _ => _.Items, _ => _.Item);
            }
        }

        public async Task<Playlist> CreatePodcastList(int episodeId)
        {
            var episode = await repo.FindAsync<Episode>(episodeId);
            if (episode == null)
            {
                return null;
            }

            var podcastList = new Playlist(episode);
            podcastList.SetCurrent(episode);
            playlistCache.Add(podcastList);

            return podcastList;
        }

        public async Task Add(Playlist playlist)
        {
            await repo.AddAndSaveOrdered(playlist, _ => _.OrderNumber);
        }

        public async Task<int> MoveItem(Playlist source, int index)
        {
            return await repo.ChangeOrderAsync(source, index);
        }

        public async Task<int> AddItem(PlaylistItem playlistItem, Playlist playlist)
        {
            return await repo.AddToPlaylist(playlistItem, playlist);
        }

        public async Task Remove(Playlist playlist)
        {
            await repo.DeleteAndSaveOrdered(playlist, _ => _.OrderNumber);
        }

        private async Task SyncPlaylist(Playlist target)
        {
            await repo.UpdateValuesAndSave(target, false);
        }
    }
}