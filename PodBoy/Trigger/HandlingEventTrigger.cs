using System.Windows;

namespace PodBoy.Trigger
{
    public class HandlingEventTrigger : System.Windows.Interactivity.EventTrigger
    {
        protected override void OnEvent(System.EventArgs eventArgs)
        {
            var routedEventArgs = eventArgs as RoutedEventArgs;
            if (routedEventArgs != null)
            {
                routedEventArgs.Handled = true;
            }

            base.OnEvent(eventArgs);
        }
    }
}