using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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
            base.OnStartup(e);
            Task.Run(() => LogListener.Listen());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Interlocked.Increment(ref LogListener.Running);
            base.OnExit(e);
        }
    }
}
