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
                    SetSelected(value);
                    var descendants = GetDescendants();
                    foreach (var descendant in descendants)
                    {
                        descendant.SetSelected(value);
                    }
                    var loggers = descendants.Select(d => d.FullLoggerName)
                        .Where(f => !string.IsNullOrEmpty(f))
                        .ToArray();
                    var loggerToggleEventPayload = new LoggerToggleEventPayload
                    {
                        Loggers = loggers,
                        Selected = value
                    };
                    App.EventAggregator.Value.GetEvent<LoggerToggleEvent>().Publish(loggerToggleEventPayload);
                }
            }
        }

        private void SetSelected(bool value)
        {
            isSelected = value;
            BeginInvokePropertyChanged(nameof(IsSelected));
        }

        private List<LoggerNode> GetDescendants()
        {
            var descendants = new List<LoggerNode>();
            descendants.AddRange(childLoggersList);
            descendants.AddRange(childLoggersList.SelectMany(c => c.GetDescendants()));
            return descendants;
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
        private readonly List<LoggerNode> childLoggersList;
        public ListCollectionView ChildLoggers { get; }
        private readonly Dictionary<string, LoggerNode> childrenDictionary = new Dictionary<string, LoggerNode>();

        public LoggerNode()
        {
            childLoggersList = new List<LoggerNode>();
            ChildLoggers = new ListCollectionView(childLoggersList) { IsLiveSorting = true };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddChild(IEnumerable<string> parts, string fullLoggerName, HashSet<string> loggersAdded)
        {
            LoggerNode child;
            var firstPart = parts.First();
            if (!childrenDictionary.TryGetValue(firstPart, out child))
            {
                child = new LoggerNode { Name = firstPart, IsSelected = true, IsExpanded = true };
                childLoggersList.Add(child);
                childrenDictionary.Add(firstPart, child);
            }
            var remaining = parts.Skip(1);
            if (remaining.Any())
            {
                child.AddChild(remaining, fullLoggerName, loggersAdded);
            }
            else
            {
                child.FullLoggerName = fullLoggerName;
                loggersAdded.Add(fullLoggerName);
            }
            ChildLoggers.Refresh();
        }

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }
    }
}
