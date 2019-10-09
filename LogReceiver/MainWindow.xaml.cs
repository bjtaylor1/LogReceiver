using Prism.Events;
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
        public MainWindow()
        {
            InitializeComponent();
            var eventAggregator = new EventAggregator();
            MainViewModel mainViewModel = new MainViewModel(eventAggregator);
            DataContext = mainViewModel;
            Task.Run(() => LogListener.Listen(eventAggregator));
        }

        private void DataGridRow_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
    }
}
