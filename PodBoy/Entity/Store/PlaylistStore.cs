using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using PodBoy.Context;
using PodBoy.Playlists;

namespace PodBoy.Entity.Store
{
    public class PlaylistStore : OrderedEntityStore<Playlist>, IPlaylistStore
    {
        public PlaylistStore()
        {
            Cache.ItemChanged.Where(_ => _.Sender.Id != 0)
                .Where(_ => _.PropertyName == nameof(Playlist.Name) || _.PropertyName == nameof(Playlist.Current))
                .Skip(1)
                .Throttle(TimeSpan.FromSeconds(0.5))
                .Subscribe(async _ => await UpdatePlaylist(_.Sender));
        }

        private async Task UpdatePlaylist(Playlist playlist)
        {
            using (var repo = RepositoryFactory.Create())
            {
                await repo.UpdateAsyncAndSave(playlist, false);
            }
        }

        public override async Task LoadSingleAsync(Playlist entity)
        {
            if (entity.Id > 0)
            {
                using (var repo = RepositoryFactory.Create())
                {
                    await LoadSingleAsync(entity, repo);
                }
            }
        }

        private async Task LoadSingleAsync(Playlist entity, IPodboyRepository repo)
        {
            await repo.LoadRelatedCollection(entity, _ => _.Items, _ => _.Item.Channel);
        }

        protected override void OnAddedToCache(Playlist playlist)
        {
            if (playlist.Type == PlaylistType.Podcast)
            {
                playlist.ItemsCount = playlist.Items.Count;
            }
            else
            {
                using (var repo = RepositoryFactory.Create())
                {
                    playlist.ItemsCount = repo.Count<PlaylistItem>(item => item.PlaylistId == playlist.Id);
                }
            }
        }

        public Playlist CreatePodcastList(params Episode[] episodes)
        {
            var podcastList = new Playlist(episodes);

            var cachedPlaylist = Cache.FirstOrDefault(_ => _.Type == PlaylistType.Podcast);
            if (cachedPlaylist != null)
            {
                Cache.Remove(cachedPlaylist);
            }
            AddToCache(podcastList);

            return podcastList;
        }

        public async Task<Playlist> CreatePodcastList(params int[] episodeIds)
        {
            Episode[] episodes;
            using (var repo = RepositoryFactory.Create())
            {
                episodes =
                    await repo.Where<Episode>(_ => episodeIds.Contains(_.Id)).Include(_ => _.Channel).ToArrayAsync();
            }
            if (episodes == null)
            {
                return null;
            }

            return CreatePodcastList(episodes);
        }

        public async Task<int> AddItem(int episodeId, Playlist playlist)
        {
            if (episodeId < 1)
            {
                return 0;
            }

            using (var repo = RepositoryFactory.Create())
            {
                repo.Attach(playlist);
                var episode = await repo.FindAsync<Episode>(episodeId);

                var newItem = new PlaylistItem(episode);

                playlist.AddItem(newItem);
                playlist.ItemsCount = playlist.Items.Count;

                var result =
                    await repo.AddAndSaveOrdered(newItem, _ => _.OrderNumber, _ => _.Playlist.Id == playlist.Id);
                return result;
            }
        }

        public async Task<int> ChangeItemOrder(PlaylistItem source, int index)
        {
            using (var repo = RepositoryFactory.Create())
            {
                return await repo.ChangeOrderAsync(source, index, _ => _.PlaylistId == source.PlaylistId);
            }
        }

        public async Task<int> ChangeItemOrderAsync(PlaylistItem item,
            int newOrderNumber,
            Expression<Func<PlaylistItem, bool>> filter = null)
        {
            if (item.Playlist != null && !Cache.Contains(item.Playlist))
            {
                AddToCache(item.Playlist);
            }

            using (var repo = RepositoryFactory.Create())
            {
                return await repo.ChangeOrderAsync(item, newOrderNumber, filter);
            }
        }

        public async Task<int> RemoveItem(PlaylistItem item)
        {
            using (var repo = RepositoryFactory.Create())
            {
                var targetList = item.Playlist;

                if (!Cache.Contains(targetList))
                {
                    targetList = await repo.FindAsync<Playlist>(targetList.Id);
                    AddToCache(targetList);
                }
                repo.Attach(targetList);
                repo.Attach(item);

                targetList.RemoveItem(item);
                targetList.ItemsCount = targetList.Items.Count;

                var result =
                    await repo.DeleteAndSaveOrdered(item, _ => _.OrderNumber, _ => _.Playlist.Id == item.PlaylistId);

                await repo.SaveAsync();

                return result;
            }
        }

        public async Task<int> RemoveItemsByChannel(Channel channel)
        {
            using (var repo = RepositoryFactory.Create())
            {
                await repo.LoadRelatedCollection(channel, _ => _.Episodes);

                // determine affected playlist items
                var episodeIds = channel.Episodes.Select(_ => _.Id);
                var payloadGroup =
                    await
                        repo.Where<PlaylistItem>(p => episodeIds.Contains(p.ItemId))
                            .GroupBy(_ => _.PlaylistId)
                            .Select(_ => new
                            {
                                PlaylistId = _.Key,
                                ItemIds = _.Select(item => item.Id).ToList()
                            }).ToListAsync();

                foreach (var payload in payloadGroup)
                {
                    repo.DeleteRangeOrdered<PlaylistItem>(payload.ItemIds, _ => _.OrderNumber,
                        _ => _.PlaylistId == payload.PlaylistId);
                }

                // delete items
                var result = await repo.SaveAsync();

                await InvalidateCache(repo, payloadGroup.Select(_ => _.PlaylistId));

                return result;
            }
        }

        private async Task InvalidateCache(IPodboyRepository repo, IEnumerable<int> keys)
        {
            var invalidate = Cache.Where(_ => keys.Contains(_.Id)).ToList();
            if (invalidate.Any())
            {
                Cache.RemoveAll(invalidate);

                await GetAllAsync(repo);

                // signal reset
                resetSignal.OnNext(Unit.Default);
            }
        }

        public async Task UpdateEpisodeAsync(Episode episode)
        {
            using (var repo = RepositoryFactory.Create())
            {
                await repo.UpdateAsyncAndSave(episode, false);
            }
        }

        public void UpdateEpisode(Episode episode)
        {
            using (var repo = RepositoryFactory.Create())
            {
                repo.Update(episode);
                repo.Save();
            }
        }
    }
}