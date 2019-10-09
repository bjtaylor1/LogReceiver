using Prism.Events;
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
            mainViewModel.AddLoggerRoot("Parent.Child.Grandchild");
            DataContext = mainViewModel;
            Task.Run(() => LogListener.Listen(eventAggregator));
        }
    }
}
