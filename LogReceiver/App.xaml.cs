using Prism.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LogReceiver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check for single instance
            if (!SingleInstanceManager.IsFirstInstance())
            {
                // Another instance is already running, exit this one
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Interlocked.Increment(ref LogListener.Running);
            LogListener.Stop();
            SingleInstanceManager.ReleaseMutex();
            base.OnExit(e);
        }

        public static Lazy<EventAggregator> EventAggregator { get; } = new Lazy<EventAggregator>();
    }
}
