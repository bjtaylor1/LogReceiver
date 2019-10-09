using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace LogReceiver
{
    public class LoggerNode : INotifyPropertyChanged
    {
        private bool isSelected;
        private string fullLoggerName;

        public string Name { get; set; }

        public string FullLoggerName
        {
            get => fullLoggerName; set
            {
                fullLoggerName = value;
                PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(nameof(FullLoggerName)), null, null);
            }
        }

        public bool IsSelected
        {
            get => isSelected; set
            {
                isSelected = value;
                PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(nameof(IsSelected)), null, null);
                foreach (var child in categories)
                {
                    child.IsSelected = value;
                }
            }
        }

        //need to be kept in sync
        private readonly List<LoggerNode> categories;
        public ListCollectionView ChildLoggers { get; }
        private readonly Dictionary<string, LoggerNode> childrenDictionary = new Dictionary<string, LoggerNode>();

        public LoggerNode()
        {
            categories = new List<LoggerNode>();
            ChildLoggers = new ListCollectionView(categories) { IsLiveSorting = true };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddChild(IEnumerable<string> parts, string fullLoggerName)
        {
            LoggerNode child;
            var firstPart = parts.First();
            if (!childrenDictionary.TryGetValue(firstPart, out child))
            {
                child = new LoggerNode { Name = firstPart, IsSelected = true };
                categories.Add(child);
                childrenDictionary.Add(firstPart, child);
            }
            var remaining = parts.Skip(1);
            if (remaining.Any())
            {
                child.AddChild(remaining, fullLoggerName);
            }
            else
            {
                child.FullLoggerName = fullLoggerName;
            }
            ChildLoggers.Refresh();
        }
    }
}
