using System;
using PodBoy.Feed;

namespace PodBoy
{
    public class EpisodeInfo
    {
        //public EpisodeInfo(string id, DateTime? date)
        //{
        //    Id = id;
        //    Date = date;
        //}

        public string Id { get; set; }
        public DateTime? Date { get; set; }

        public bool IsEquivalentTo(ItunesItem item)
        {
            if (item.Id == null)
            {
                return Date == item.PublishDate;
            }

            return Id == item.Id;
        }
    }
}