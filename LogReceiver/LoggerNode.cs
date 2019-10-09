using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace LogReceiver
{
    public class LoggerNode : INotifyPropertyChanged
    {
        private bool isSelected, isExpanded;
        private string fullLoggerName;

        public string Name { get; set; }

        public string FullLoggerName
        {
            get => fullLoggerName; set
            {
                fullLoggerName = value;
                BeginInvokePropertyChanged(nameof(FullLoggerName));
            }
        }

        public bool IsSelected
        {
            get => isSelected; set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    BeginInvokePropertyChanged(nameof(IsSelected));
                    foreach (var child in categories)
                    {
                        child.IsSelected = value;
                    }
                }
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    BeginInvokePropertyChanged(nameof(IsExpanded));
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
                child = new LoggerNode { Name = firstPart, IsSelected = true, IsExpanded = true };
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

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }
    }
}
