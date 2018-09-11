using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PodBoy.Virtualization
{
    public class DispatcherTaskScheduler : TaskScheduler
    {
        private readonly Dispatcher dispatcher;
        private readonly DispatcherPriority priority;

        public DispatcherTaskScheduler(Dispatcher dispatcher, DispatcherPriority priority)
        {
            this.dispatcher = dispatcher;
            this.priority = priority;
        }

        protected override void QueueTask(Task task)
        {
            dispatcher.BeginInvoke(new Action(() => TryExecuteTask(task)), priority);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // don't support inlining; inling would make sense if somebody blocked
            // the UI thread waiting for a Task that was scheduled on this scheduler
            // and we wanted to avoid the deadlock
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // this is only useful for debugging, so we can ignore it
            throw new NotSupportedException();
        }
    }
}