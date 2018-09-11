using System;
using System.Windows.Media.Imaging;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using PodBoy;
using PodBoy.Entity;
using Tests.ReactiveUI.Testing;
using Xunit;

namespace Tests.Entity
{
    public class ImageProviderTests
    {
        public IResourceService ResourceService { get; set; }

        [Fact]
        public void CreatesImageFromUrl()
        {
            var imageUri = new Uri(@"..\..\Assets\test.png", UriKind.Relative);
            var requestedImage = new BitmapImage(imageUri);
            var defaultImage = new BitmapImage(imageUri);

            var provider = Substitute.For<ImageProvider>(imageUri, 0, BitmapCreateOptions.None);
            provider.DefaultImage.Returns(defaultImage);
            provider.CreateImage().Returns(requestedImage);

            new TestScheduler().With(s =>
            {
                provider.Image.Should().Be(defaultImage);
                s.AdvanceByMs(1);
                provider.Image.Should().Be(requestedImage);
                provider.IsDefault.Should().BeFalse();
            });
        }

        [Fact]
        public void FallsBackToDefaultImage()
        {
            var imageUri = new Uri(@"..\..\Assets\test.png", UriKind.Relative);
            var defaultImage = new BitmapImage(imageUri);

            var provider = Substitute.For<ImageProvider>(0, BitmapCreateOptions.None);
            provider.DefaultImage.Returns(defaultImage);

            provider.Image.Should().Be(defaultImage);
            provider.IsDefault.Should().BeTrue();
        }
    }
}