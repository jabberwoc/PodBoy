using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using FirstFloor.ModernUI.Windows.Controls;
using PodBoy.Bootstrap;
using Splat;

namespace PodBoy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
#if DEBUG
            var dir = new DirectoryInfo(".").Parent?.Parent?.Parent;
            if (dir != null)
            {
                AppDomain.CurrentDomain.SetData("DataDirectory", dir.FullName);
            }
#endif
        }

        public ILogger Log { get; private set; }

        public bool AlertIsOpen { get; private set; }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                RegisterUnhandledExceptionHandlers();
            }

            bootstrapper = new AppBootstrapper();
            bootstrapper.Run();

            Log = Locator.Current.GetService<ILogger>();
        }

        private AppBootstrapper bootstrapper;

        private void RegisterUnhandledExceptionHandlers()
        {
            Observable.FromEventPattern<UnhandledExceptionEventHandler, UnhandledExceptionEventArgs>(
                _ => AppDomain.CurrentDomain.UnhandledException += _,
                _ => AppDomain.CurrentDomain.UnhandledException -= _)
                .Where(_ => !AlertIsOpen)
                .Subscribe(_ => OnAppDomainUnhandledException(_.EventArgs));

            Observable.FromEventPattern<DispatcherUnhandledExceptionEventHandler, DispatcherUnhandledExceptionEventArgs>
                (_ => Current.DispatcherUnhandledException += _, _ => Current.DispatcherUnhandledException -= _)
                .Where(_ => !AlertIsOpen)
                .Subscribe(_ => OnDispatcherUnhandledException(_.EventArgs));
        }

        private void OnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            LogAndAlert(e.Exception);

            Current.Shutdown();
        }

        private void OnAppDomainUnhandledException(UnhandledExceptionEventArgs e)
        {
            Log?.Write($"Unhandled Exception occured: {e.ExceptionObject}", LogLevel.Error);
        }

        private void LogAndAlert(Exception e)
        {
            string stacktrace = e.StackTrace;

            while (e.InnerException != null) e = e.InnerException;

            var sb = new StringBuilder();
            sb.Append(e.Message);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(stacktrace);

            Log?.Write($"Unhandled Exception occured: {e}", LogLevel.Error);

            AlertIsOpen = true;

            // TODO
            ModernDialog.ShowMessage(sb.ToString(), "Error", MessageBoxButton.OK);

            AlertIsOpen = false;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            bootstrapper.Dispose();
        }
    }
}