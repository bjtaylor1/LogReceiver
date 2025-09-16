using Prism.Events;
using System;
using System.Diagnostics;
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
            // Console.WriteLine will automatically work if started from command line
            // and will be ignored if started from GUI (no console attached)
            Console.WriteLine("LogReceiver starting - Debug output enabled");
            Console.WriteLine("=" + new string('=', 50));

            // Check for single instance
            if (!SingleInstanceManager.IsFirstInstance())
            {
                // Another instance is already running, exit this one
                Console.WriteLine("Another instance of LogReceiver is already running.");
                
                // Try console first, fallback to MessageBox
                Console.WriteLine("Press any key to exit...");
                var task = Task.Run(() => {
                    try { Console.ReadKey(); } catch { }
                });
                
                if (!task.Wait(2000)) // Wait 2 seconds for console input
                {
                    // Probably no console, show MessageBox instead
                    MessageBox.Show("Another instance of LogReceiver is already running.", 
                                  "LogReceiver", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
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
            
            Console.WriteLine("LogReceiver shutting down...");
            
            base.OnExit(e);
        }

        public static Lazy<EventAggregator> EventAggregator { get; } = new Lazy<EventAggregator>();
    }
}
