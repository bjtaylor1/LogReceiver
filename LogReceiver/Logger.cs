using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace LogReceiver
{
    public class Logger : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Name { get; set; }

        public bool IsSelected
        {
            get => isSelected; set
            {
                isSelected = value;
                PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(nameof(IsSelected)), null, null);
                foreach(var child in categories)
                {
                    child.IsSelected = value;
                }
            }
        }

        //need to be kept in sync
        private readonly List<Logger> categories;
        public ListCollectionView ChildLoggers { get; }
        private readonly Dictionary<string, Logger> childrenDictionary = new Dictionary<string, Logger>();

        public Logger()
        {
            categories = new List<Logger>();
            ChildLoggers = new ListCollectionView(categories) { IsLiveSorting = true };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddChild(IEnumerable<string> parts)
        {
            Logger child;
            var firstPart = parts.First();
            if (!childrenDictionary.TryGetValue(firstPart, out child))
            {
                child = new Logger { Name = firstPart, IsSelected = true };
                categories.Add(child);
                childrenDictionary.Add(firstPart, child);
            }
            var remaining = parts.Skip(1);
            if(remaining.Any())
            {
                child.AddChild(remaining);
            }
        }
    }
}
