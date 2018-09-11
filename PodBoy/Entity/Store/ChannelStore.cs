using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using MoreLinq;
using PodBoy.Context;

namespace PodBoy.Entity.Store
{
    public class ChannelStore : OrderedEntityStore<Channel>, IChannelStore
    {
        public override async Task LoadSingleAsync(Channel entity)
        {
            if (entity.Id > 0)
            {
                using (var repo = RepositoryFactory.Create())
                {
                    await LoadSingleAsync(entity, repo);
                }
            }
        }

        private async Task LoadSingleAsync(Channel entity, IPodboyRepository repo)
        {
            await repo.LoadRelatedCollection(entity, _ => _.Episodes);
        }

        public override async Task<List<Channel>> GetAllAsync(Func<Channel, bool> predicate = null)
        {
            using (var repo = RepositoryFactory.Create())
            {
                var channels = await base.GetAllAsync(repo, predicate);
                UpdateUnplayedCount(repo);
                return channels;
            }
        }

        public async Task MarkChannelPlayed(Channel channel)
        {
            using (var repo = RepositoryFactory.Create())
            {
                await repo.Where<Episode>(_ => _.ChannelId == channel.Id).ForEachAsync(_ => _.IsPlayed = true);
                await repo.SaveAsync();

                await UpdateUnplayedCount(channel.Id, repo);
            }
        }

        public async Task UpdateUnplayedCount(int channelId)
        {
            using (var repo = RepositoryFactory.Create())
            {
                var channel = Cache.FirstOrDefault(_ => _.Id == channelId) ?? await FindAsync(channelId, repo);

                if (channel == null)
                {
                    return;
                }

                AddToCache(channel);

                await UpdateUnplayedCount(channel.Id, repo);
            }
        }

        private async Task UpdateUnplayedCount(int channelId, IPodboyRepository repo)
        {
            if (channelId == 0)
            {
                return;
            }

            var channel = await GetSingleAsync(channelId);
            if (channel == null)
            {
                return;
            }
            channel.UnplayedCount = await repo.CountAsync<Episode>(e => e.ChannelId == channelId && e.IsPlayed == false);
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannelInfos()
        {
            using (var repo = RepositoryFactory.Create())
            {
                return await repo.Include<Channel>(_ => _.Episodes).Select(_ => new ChannelInfo
                {
                    Id = _.Id,
                    FeedUrl = _.Link,
                    ImageUrl = _.ImageUrl,
                    EpisodeInfos = _.Episodes.Select(x => new EpisodeInfo
                    {
                        Id = x.Guid,
                        Date = x.Date
                    })
                }).ToListAsync();
            }
        }

        public async Task UpdateEpisode(Episode episode)
        {
            using (var repo = RepositoryFactory.Create())
            {
                await repo.UpdateAsyncAndSave(episode, false);
                await UpdateUnplayedCount(episode.ChannelId, repo);
            }
        }

        public async Task<int> SaveEpisodes(IEnumerable<Episode> episodes)
        {
            using (var repo = RepositoryFactory.Create())
            {
                var result = await repo.AddRangeAndSave(episodes);

                UpdateUnplayedCount(repo);
                return result;
            }
        }

        private void UpdateUnplayedCount(IPodboyRepository repo)
        {
            Cache.Where(_ => _.Id != 0)
                .ForEach(
                    async _ =>
                        _.UnplayedCount =
                            await repo.CountAsync<Episode>(e => e.ChannelId == _.Id && e.IsPlayed == false));
        }

        public override async Task<int> Remove(Channel entity)
        {
            int result;
            using (var repo = RepositoryFactory.Create())
            {
                await LoadSingleAsync(entity, repo);

                // delete channel
                result = await repo.DeleteAndSaveOrdered(entity, _ => _.OrderNumber);

                await repo.SaveAsync();
            }

            if (result > 0)
            {
                Cache.Remove(entity);
                resetSignal.OnNext(Unit.Default);
            }

            return result;
        }

        public override async Task<int> Add(Channel entity)
        {
            using (var repo = RepositoryFactory.Create())
            {
                var result = await Add(entity, repo);
                if (result > 0)
                {
                    await UpdateUnplayedCount(entity.Id, repo);
                }
                return result;
            }
        }
    }
}