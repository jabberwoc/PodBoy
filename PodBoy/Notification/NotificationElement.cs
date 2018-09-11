using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace PodBoy.Notification
{
    public class NotificationElement : ReactiveObject
    {
        private bool isActive;
        private bool isFading;

        public NotificationElement(NotificationType type, string message)
        {
            Type = type;
            Message = message;
            DateCreated = DateTime.Now;

            this.WhenAnyValue(_ => _.IsActive).Where(active => !active).Subscribe(_ => IsFading = false);
        }

        public NotificationType Type { get; }
        public string Message { get; }
        public DateTime DateCreated { get; }

        public bool IsActive
        {
            get { return isActive; }
            set { this.RaiseAndSetIfChanged(ref isActive, value); }
        }

        public bool IsFading
        {
            get { return isFading; }
            set { this.RaiseAndSetIfChanged(ref isFading, value); }
        }

        public IDisposable IsNotSelected { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var ne = (NotificationElement) obj;
            return Equals(ne);
        }

        private bool Equals(NotificationElement ne)
        {
            return Type == ne.Type && DateCreated == ne.DateCreated && Equals(Message, ne.Message);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Type.GetHashCode();
            hash = hash * 23 + Message.GetHashCode();
            hash = hash * 23 + DateCreated.GetHashCode();
            return hash;
        }
    }
}