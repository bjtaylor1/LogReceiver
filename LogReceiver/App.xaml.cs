using Prism.Events;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            // Always allocate console for debug output
            AllocConsole();
            Console.WriteLine("LogReceiver Console Mode - Debug output will appear here");
            Console.WriteLine("Close this console window or press Ctrl+C to exit the application");
            Console.WriteLine("=" + new string('=', 60));

            // Check for single instance
            if (!SingleInstanceManager.IsFirstInstance())
            {
                // Another instance is already running, exit this one
                Console.WriteLine("Another instance of LogReceiver is already running.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
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
            
            // Clean up console if it was allocated
            try
            {
                FreeConsole();
            }
            catch { /* Ignore if console wasn't allocated */ }
            
            base.OnExit(e);
        }

        public static Lazy<EventAggregator> EventAggregator { get; } = new Lazy<EventAggregator>();
    }
}
