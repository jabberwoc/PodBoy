using System;
using System.Reactive;
using System.Reactive.Linq;
using Reactive.EventAggregator;
using ReactiveUI;

namespace PodBoy.Notification
{
    public class NotificationViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isVisible;
        private ReactiveList<NotificationElement> notifications;
        private IReactiveDerivedList<NotificationElement> notificationView;
        private NotificationElement selectedElement;

        public readonly TimeSpan notificationDelay = TimeSpan.FromMilliseconds(250);
        public readonly TimeSpan timeoutDelay = TimeSpan.FromSeconds(5);

        public NotificationViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<NotificationEvent>().Throttle(notificationDelay, RxApp.MainThreadScheduler)
                .Subscribe(OnNotificationEvent);
            Notifications.IsEmptyChanged.Select(_ => !_).ToProperty(this, _ => _.IsVisible, out isVisible);

            RemoveNotificationCommand = ReactiveCommand.Create<NotificationElement>(RemoveActiveNotification);
        }

        public bool IsVisible => isVisible.Value;

        public ReactiveList<NotificationElement> Notifications
        {
            get => notifications ?? (notifications = new ReactiveList<NotificationElement>
            {
                ChangeTrackingEnabled = true
            });
            set => this.RaiseAndSetIfChanged(ref notifications, value);
        }

        public IReactiveDerivedList<NotificationElement> NotificationView
        {
            get
            {
                return notificationView
                       ?? (notificationView = Notifications.CreateDerivedCollection(x => x, e => e.IsActive));
            }
            set => this.RaiseAndSetIfChanged(ref notificationView, value);
        }

        public NotificationElement SelectedElement
        {
            get => selectedElement;
            set => this.RaiseAndSetIfChanged(ref selectedElement, value);
        }

        public ReactiveCommand<NotificationElement, Unit> RemoveNotificationCommand { get; set; }

        private void OnNotificationEvent(NotificationEvent @event)
        {
            var notification = @event.Notification;
            notification.IsActive = true;
            Notifications.Add(notification);

            // activate timer when not selected
            notification.IsNotSelected =
                this.WhenAnyValue(_ => _.SelectedElement).Where(_ => !Equals(_, notification)).Subscribe(
                    _ =>
                        Observable.Timer(timeoutDelay, RxApp.MainThreadScheduler).Subscribe(
                            x => OnFadeOutTimer(notification)));
        }

        private void RemoveActiveNotification(NotificationElement notification)
        {
            notification.IsFading = true;
            notification.IsNotSelected.Dispose();
        }

        private void OnFadeOutTimer(NotificationElement notification)
        {
            if (!Equals(SelectedElement, notification))
            {
                RemoveActiveNotification(notification);
            }
        }
    }
}