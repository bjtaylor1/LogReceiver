using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogReceiver
{
    public class MainViewModel
    {
        public ObservableCollection<Event> Events { get; } = new ObservableCollection<Event>();

        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
    }

    public class Category
    {
        public string Name { get; set; }
        public ObservableCollection<Category> Children { get; } = new ObservableCollection<Category>();
    }
}
