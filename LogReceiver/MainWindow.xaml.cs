using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace LogReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Task listenTask;
        private readonly MainViewModel mainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            mainViewModel = new MainViewModel();
            DataContext = mainViewModel;
            listenTask = Task.Run(LogListener.Listen);
            Closing += HandleClosing;
        }

        private void HandleClosing(object? sender, CancelEventArgs e)
        {
            LogListener.Stop();
            if (!LogListener.StoppedEvent.Wait(TimeSpan.FromSeconds(2)))
            {
                MessageBox.Show(this, "The listener didn't report having stopped. Please check the process has fully exited.",
                    "LogReceiver", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else Debug.WriteLine("The listener reported stopped.");
        }

        private void LevelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Content is string level)
            {
                mainViewModel.OnLevelSelectionChanged(level, true);
            }
        }

        private void LevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Content is string level)
            {
                mainViewModel.OnLevelSelectionChanged(level, false);
            }
        }
        private void DataGrid_Documents_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;      
        }
    }
}
