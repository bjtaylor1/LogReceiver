using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace LogReceiver
{
    /// <summary>
    /// Manages single instance application behavior
    /// </summary>
    public static class SingleInstanceManager
    {
        private static Mutex _mutex;
        private const string MutexName = "LogReceiver_SingleInstance_Mutex";
        private const string WindowTitle = "Log receiver";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// Checks if this is the first instance of the application
        /// </summary>
        /// <returns>True if this is the first instance, false if another instance is already running</returns>
        public static bool IsFirstInstance()
        {
            bool isFirstInstance;
            _mutex = new Mutex(true, MutexName, out isFirstInstance);

            if (!isFirstInstance)
            {
                // Try to bring the existing window to the foreground
                BringExistingInstanceToForeground();
            }

            return isFirstInstance;
        }

        /// <summary>
        /// Attempts to find and bring the existing application window to the foreground
        /// </summary>
        private static void BringExistingInstanceToForeground()
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id != currentProcess.Id)
                    {
                        IntPtr hWnd = process.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            // If the window is minimized, restore it
                            if (IsIconic(hWnd))
                            {
                                ShowWindow(hWnd, SW_RESTORE);
                            }

                            // Bring the window to the foreground
                            SetForegroundWindow(hWnd);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error bringing existing instance to foreground: {ex.Message}");
            }
        }

        /// <summary>
        /// Releases the mutex when the application closes
        /// </summary>
        public static void ReleaseMutex()
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error releasing mutex: {ex.Message}");
            }
        }
    }
}
