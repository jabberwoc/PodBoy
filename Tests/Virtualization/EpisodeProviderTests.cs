using FluentAssertions;
using PodBoy.Entity;
using PodBoy.Virtualization;
using ReactiveUI;
using Xunit;

namespace Tests.Virtualization
{
    public class EpisodeProviderTests
    {
        // TODO refactor
        //[Fact]
        //public void FetchCountTest()
        //{
        //    var collection = new ReactiveList<Episode>
        //    {
        //        new Episode()
        //    };
        //    var provider = new RepoProvider(collection.CreateDerivedCollection(_ => _));
        //    provider.FetchCount().Should().Be(collection.Count);
        //}

        //[Fact]
        //public void FetchRangeTest()
        //{
        //    var episode1 = new Episode
        //    {
        //        Title = "1"
        //    };
        //    var episode2 = new Episode
        //    {
        //        Title = "2"
        //    };
        //    var episode3 = new Episode
        //    {
        //        Title = "3"
        //    };
        //    var collection = new ReactiveList<Episode>();
        //    collection.AddRange(new[]
        //    {
        //        episode1,
        //        episode2,
        //        episode3
        //    });

        //    var provider = new RepoProvider(collection.CreateDerivedCollection(_ => _));
        //    provider.FetchRange(1, 2).ShouldBeEquivalentTo(new[]
        //    {
        //        episode2,
        //        episode3
        //    });
        //}
    }
}