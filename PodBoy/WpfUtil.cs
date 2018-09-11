using System.Windows.Threading;

namespace PodBoy
{
    public class WpfUtil
    {
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(f =>
            {
                ((DispatcherFrame) f).Continue = false;
                return null;
            }), frame);
            Dispatcher.PushFrame(frame);
        }
    }
}