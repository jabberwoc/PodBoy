using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using PodBoy.Notification;
using Reactive.EventAggregator;
using Tests.ReactiveUI.Testing;
using Xunit;

namespace Tests.ViewModel
{
    public class NotificationViewModelTests
    {
        public NotificationViewModelTests()
        {
            EventAggregator = Substitute.For<EventAggregator>();
        }

        public IEventAggregator EventAggregator { get; }

        [Fact]
        public void ActiveNotificationThrottleTest()
        {
            // two notifications
            var firstNotification = new NotificationElement(NotificationType.Info, @"Test");
            var secondNotification = new NotificationElement(NotificationType.Info, @"Test");

            new TestScheduler().With(s =>
            {
                var viewModel = Substitute.For<NotificationViewModel>(EventAggregator);

                EventAggregator.Publish(new NotificationEvent
                {
                    Notification = firstNotification
                });

                EventAggregator.Publish(new NotificationEvent
                {
                    Notification = secondNotification
                });

                // advance by throttle delay
                s.AdvanceByMs(viewModel.notificationDelay.TotalMilliseconds);

                firstNotification.IsActive.Should().BeFalse("it was ignored due to throttle.");
                secondNotification.IsActive.Should().BeTrue();
                viewModel.IsVisible.Should().BeTrue();
            });
        }

        [Fact]
        public void ActiveNotificationsShouldFadeOut()
        {
            // two notifications
            var notification = new NotificationElement(NotificationType.Info, @"Test");

            new TestScheduler().With(s =>
            {
                var viewModel = Substitute.For<NotificationViewModel>(EventAggregator);

                EventAggregator.Publish(new NotificationEvent
                {
                    Notification = notification
                });

                s.AdvanceByMs(viewModel.notificationDelay.TotalMilliseconds);

                notification.IsActive.Should().BeTrue();

                s.AdvanceByMs(viewModel.timeoutDelay.TotalMilliseconds);

                notification.IsFading.Should().BeTrue();
            });
        }

        [Fact]
        public void NotificationSelectionPreventsFadeOut()
        {
            // two notifications
            var notification = new NotificationElement(NotificationType.Info, @"Test");

            new TestScheduler().With(s =>
            {
                var viewModel = Substitute.For<NotificationViewModel>(EventAggregator);

                EventAggregator.Publish(new NotificationEvent
                {
                    Notification = notification
                });

                s.AdvanceByMs(viewModel.notificationDelay.TotalMilliseconds);

                notification.IsActive.Should().BeTrue();

                // select notification
                viewModel.SelectedElement = notification;

                s.AdvanceByMs(viewModel.timeoutDelay.TotalMilliseconds);

                notification.IsFading.Should().BeFalse();

                // reset selection
                viewModel.SelectedElement = null;

                s.AdvanceByMs(viewModel.timeoutDelay.TotalMilliseconds);

                notification.IsFading.Should().BeTrue();
            });
        }
    }
}