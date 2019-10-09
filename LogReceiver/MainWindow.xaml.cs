using Prism.Events;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScrollViewer dataGridScrollViewer;

        public MainWindow()
        {
            InitializeComponent();
            var eventAggregator = new EventAggregator();
            MainViewModel mainViewModel = new MainViewModel(eventAggregator);
            DataContext = mainViewModel;
            Task.Run(() => LogListener.Listen(eventAggregator));
            dataGrid.Loaded += HandleDataGridLoaded;
        }

        private void HandleDataGridLoaded(object sender, RoutedEventArgs e)
        {
            dataGridScrollViewer = GetScrollViewer(dataGrid);
            var items = (INotifyCollectionChanged)dataGrid.Items;
            items.CollectionChanged += HandleDataGridCollectionChanged;
        }

        private void HandleDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            dataGridScrollViewer?.ScrollToEnd();
        }

        public static ScrollViewer GetScrollViewer(UIElement element)
        {
            if (element == null) return null;

            ScrollViewer sv = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element) && sv == null; i++)
            {
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
                {
                    sv = (ScrollViewer)(VisualTreeHelper.GetChild(element, i));
                }
                else
                {
                    sv = GetScrollViewer(VisualTreeHelper.GetChild(element, i) as UIElement);
                }
            }
            return sv;
        }

        private void ItemAdded()
        {
            dataGrid.ScrollIntoView(dataGrid.Items[dataGrid.Items.Count - 1]);
        }

        private void DataGridRow_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
    }
}
