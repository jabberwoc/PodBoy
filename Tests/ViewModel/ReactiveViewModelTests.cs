using NSubstitute;
using PodBoy;
using PodBoy.Extension;
using PodBoy.Notification;
using PodBoy.ViewModel;
using Reactive.EventAggregator;
using Splat;
using Xunit;

namespace Tests.ViewModel
{
    public class ReactiveViewModelTests
    {
        public ReactiveViewModelTests()
        {
            ViewModel = new ReactiveTestViewModel(Substitute.For<IEventAggregator>());
        }

        public ReactiveTestViewModel ViewModel { get; }

        [Fact]
        public void LogAndNotifyTest()
        {
            const NotificationType type = NotificationType.Info;
            const string logMessage = "log";
            const string notificationMessage = "notification";

            var logger = Substitute.For<DefaultLogger>();
            Locator.CurrentMutable.RegisterConstant(logger, typeof(ILogger));

            ViewModel.LogAndNotify(type, logMessage, notificationMessage);

            // message logged
            logger.Received(1).Write(Arg.Is("{0}: {1}".FormatString(typeof(ReactiveTestViewModel).Name, logMessage)),
                Arg.Is(LogLevel.Info));

            // notification event published
            ViewModel.EventAggregator.Received(1).Publish(
                Arg.Is<NotificationEvent>(
                    n => Equals(n.Notification.Type, type) && Equals(n.Notification.Message, notificationMessage)));
        }

        public class ReactiveTestViewModel : ReactiveViewModel
        {
            public ReactiveTestViewModel(IEventAggregator eventAggregator)
                : base(eventAggregator) {}
        }
    }
}