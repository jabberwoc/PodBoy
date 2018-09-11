using System;
using FluentAssertions;
using PodBoy.Entity;
using Xunit;

namespace Tests.Entity
{
    public class EpisodeTests
    {
        [Fact]
        public void ImageUrlSetsProviderUri()
        {
            var imageUrl = "http://dummyUrl";
            var imageUri = new Uri(imageUrl, UriKind.Absolute);
            var channel = new Episode
            {
                ImageUrl = imageUrl
            };

            channel.ImageProvider.ImageUri.Should().Be(imageUri);
            channel.ImageUri.Should().Be(channel.ImageProvider.ImageUri);
        }
    }
}