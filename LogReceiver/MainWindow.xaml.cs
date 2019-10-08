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
            MainViewModel mainViewModel = new MainViewModel();
            var parent = new Category { Name = "Parent" };
            var child1 = new Category { Name = "Child1" };
            var grandchild1 = new Category { Name = "Grandchild1" };
            var grandchild2 = new Category { Name = "Grandchild2" };
            child1.Children.Add(grandchild1);
            child1.Children.Add(grandchild2);
            parent.Children.Add(child1);
            mainViewModel.Categories.Add(parent);
            DataContext = mainViewModel;
        }
    }
}
