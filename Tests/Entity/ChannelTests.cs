using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using FluentAssertions;
using PodBoy.Entity;
using Xunit;

namespace Tests.Entity
{
    public class ChannelTests
    {
        private static readonly DateTime Date = DateTime.Now;

        public static IEnumerable<object[]> TestCases => new[]
        {
            new object[]
            {
                false,
                "bla",
                "blubb",
                DateTime.Now,
                DateTime.Now.AddDays(1)
            },
            new object[]
            {
                true,
                "geronimo",
                "geronimo",
                DateTime.Now,
                DateTime.Now.AddDays(1)
            },
            new object[]
            {
                true,
                null,
                null,
                Date,
                Date
            }
        };

        [Theory]
        [MemberData("TestCases")]
        public void ChannelHasEpisodeTest(bool hasEpisode,
            string id,
            string existingId,
            DateTime dateTime,
            DateTime existingDateTime)
        {
            var feedItem = new SyndicationItem
            {
                Id = id,
                PublishDate = dateTime
            };

            var channel = new Channel("dummyLink");
            channel.Episodes.Add(new Episode
            {
                Guid = existingId,
                Date = existingDateTime
            });

            channel.HasEpisode(feedItem).Should().Be(hasEpisode);
        }

        [Fact]
        public void ImageUrlSetsProviderUri()
        {
            var imageUrl = "http://dummyUrl";
            var imageUri = new Uri(imageUrl, UriKind.Absolute);
            var channel = new Channel("dummyLink")
            {
                ImageUrl = imageUrl
            };

            channel.ImageProvider.ImageUri.Should().Be(imageUri);
            channel.ImageUri.Should().Be(channel.ImageProvider.ImageUri);
        }
    }
}