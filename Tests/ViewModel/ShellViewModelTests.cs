using System;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using PodBoy;
using PodBoy.Event;
using PodBoy.Notification;
using Reactive.EventAggregator;
using ReactiveUI;
using Xunit;
using INotifyPropertyChanging = ReactiveUI.INotifyPropertyChanging;
using PropertyChangingEventArgs = ReactiveUI.PropertyChangingEventArgs;
using PropertyChangingEventHandler = ReactiveUI.PropertyChangingEventHandler;

namespace Tests.ViewModel
{
    public class ShellViewModelTests
    {
        public ShellViewModelTests()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            EventAggregator = Substitute.For<EventAggregator>();
            ShellViewModel = new ShellViewModel(Substitute.For<NotificationViewModel>(EventAggregator), EventAggregator,
                new RoutingState());
        }

        public IEventAggregator EventAggregator { get; }

        public ShellViewModel ShellViewModel { get; private set; }

        [Fact]
        public void DeactivatesViewModelStack()
        {
            var router = Substitute.For<RoutingState>();
            ShellViewModel = new ShellViewModel(Substitute.For<NotificationViewModel>(EventAggregator), EventAggregator,
                router);

            var dummyViewModel = Substitute.For<DummyViewModel>();
            router.NavigationStack.Add(dummyViewModel);

            ShellViewModel.DeactivateViewModels();

            dummyViewModel.Received(1).Deactivate();
        }

        [Fact]
        public void ShouldRaiseShortcutEvent()
        {
            bool wasRaised = false;
            EventAggregator.GetEvent<ShortcutEvent>()
                .Where(_ => _.Type == ShortcutCommandType.TogglePlay)
                .Subscribe(_ => wasRaised = true);

            ShellViewModel.RaiseKeyboardEvent(ShortcutCommandType.TogglePlay);

            wasRaised.Should().BeTrue();
        }
    }

    public class DummyViewModel : IRoutableViewModel, IDeactivatable
    {
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public void RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            throw new NotImplementedException();
        }

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public string UrlPathSegment => typeof(DummyViewModel).FullName;

        public IScreen HostScreen { get; set; }

        public void Deactivate() {}
    }
}