using System;
using PodBoy.Notification;
using Splat;

namespace PodBoy.Extension
{
    public static class LoggerExtensions
    {
        public static void LogForType(this IFullLogger logger, NotificationType notificationType, string message)
        {
            switch (notificationType)
            {
                case NotificationType.Info:
                    logger.Info(message);
                    break;
                case NotificationType.Warning:
                    logger.Warn(message);
                    break;
                case NotificationType.Error:
                    logger.Error(message);
                    break;
            }
        }

        public static IFullLogger Log(this IEnableLogger logger)
        {
            ILogManager service = Locator.Current.GetService<ILogManager>();
            if (service == null)
            {
                throw new Exception("ILogManager is null. This should never happen, your dependency resolver is broken");
            }
            return service.GetLogger(logger.GetType());
        }
    }
}