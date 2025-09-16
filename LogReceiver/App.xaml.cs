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
#if DEBUG
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        const int ATTACH_PARENT_PROCESS = -1;
#endif

        protected override void OnStartup(StartupEventArgs e)
        {
            bool hasConsole = false;
#if DEBUG
            // Try to attach to parent console (if started from command line)
            // If this fails, we were likely started from GUI
            hasConsole = AttachConsole(ATTACH_PARENT_PROCESS);
            
            if (hasConsole)
            {
                Console.WriteLine(); // Add a newline after the command prompt
                Console.WriteLine("LogReceiver starting - Debug output enabled");
                Console.WriteLine("=" + new string('=', 50));
            }
#endif

            // Check for single instance
            if (!SingleInstanceManager.IsFirstInstance())
            {
                // Another instance is already running, exit this one
                if (hasConsole)
                {
                    Console.WriteLine("Another instance of LogReceiver is already running.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                else
                {
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
            
#if DEBUG
            Console.WriteLine("LogReceiver shutting down...");
#endif
            
            base.OnExit(e);
        }

        public static Lazy<EventAggregator> EventAggregator { get; } = new Lazy<EventAggregator>();
    }
}
