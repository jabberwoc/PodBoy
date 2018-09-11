using FluentAssertions;
using PodBoy.ViewModel;
using Xunit;

namespace Tests.ViewModel
{
    public class OpenUrlViewModelTests
    {
        public OpenUrlViewModelTests()
        {
            ViewModel = new OpenUrlViewModel();
        }

        public OpenUrlViewModel ViewModel { get; }

        [Fact]
        public void ShouldValidateUrlInput()
        {
            ViewModel.UrlInput = "Invalid Url";
            ViewModel.IsObjectValid();

            ViewModel.IsValid.Should().BeFalse();

            ViewModel.UrlInput = "http://localhost";
            ViewModel.IsObjectValid();

            ViewModel.IsValid.Should().BeTrue();
        }
    }
}