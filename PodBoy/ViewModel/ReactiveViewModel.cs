using PodBoy.Extension;
using PodBoy.Notification;
using Reactive.EventAggregator;
using ReactiveUI;

namespace PodBoy.ViewModel
{
    public abstract class ReactiveViewModel : ReactiveObject
    {
        protected ReactiveViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
        }

        public IEventAggregator EventAggregator { get; }

        public void LogAndNotify(NotificationType type, string message)
        {
            LogAndNotify(type, message, message);
        }

        public void LogAndNotify(NotificationType type, string logMessage, string notificationMessage)
        {
            this.Log().LogForType(type, logMessage);

            EventAggregator.Publish(new NotificationEvent
            {
                Notification = new NotificationElement(type, notificationMessage)
            });
        }
    }
}