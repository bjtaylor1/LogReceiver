using Newtonsoft.Json;
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
        private readonly List<LoggerNode> childLoggersList = new List<LoggerNode>();

        public string Name { get; set; }

        public string FullLoggerName
        {
            get => fullLoggerName;
            set
            {
                fullLoggerName = value;
                BeginInvokePropertyChanged(nameof(FullLoggerName));
            }
        }

        protected void ClearNodes()
        {
            childLoggersList.Clear();
            childrenDictionary.Clear();
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    var descendants = GetDescendantsAndSelf();
                    foreach (var descendant in descendants)
                    {
                        descendant.SetSelected(value);
                    }
                    App.EventAggregator.Value.GetEvent<RefreshEvent>().Publish();
                }
            }
        }

        private bool isHighlighted;
        public bool IsHighlighted
        {
            get => isHighlighted; set
            {
                if (isHighlighted != value)
                {
                    isHighlighted = value;
                    this.BeginInvokePropertyChanged(nameof(IsHighlighted));
                }
            }
        }

        private void SetSelected(bool value)
        {
            isSelected = value;
            BeginInvokePropertyChanged(nameof(IsSelected));
        }

        protected List<LoggerNode> GetDescendantsAndSelf()
        {
            var descendants = new List<LoggerNode> { this };
            descendants.AddRange(ChildLoggersList);
            descendants.AddRange(ChildLoggersList.SelectMany(c => c.GetDescendantsAndSelf()));
            return descendants;
        }

        public bool IsTurnedOn(string[] parts, int index)
        {
            bool retval;
            if (index >= parts.Length)
                retval = true;
            else
            {
                if(childrenDictionary.TryGetValue(parts[index], out var child))
                {
                    if (index == parts.Length - 1)
                        retval = child.IsSelected; //we're at the tip of the tree
                    else
                        retval = child.IsTurnedOn(parts, index + 1); //there's more children to consider
                }
                else
                {
                    retval = true;// shouldn't happen, as the node should be in the tree
                }
            }
            return retval;
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

        public List<LoggerNode> ChildLoggersList
        {
            get => childLoggersList;
            set
            {
                childLoggersList.AddRange(value);
                foreach (var item in value)
                {
                    childrenDictionary.Add(item.Name, item);
                }
                ChildLoggers.Refresh();
            }
        }

        public ListCollectionView ChildLoggers { get; }

        private readonly Dictionary<string, LoggerNode> childrenDictionary = new Dictionary<string, LoggerNode>();

        public LoggerNode()
        {
            ChildLoggers = new ListCollectionView(childLoggersList);
            ChildLoggers.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddChild(string[] parts, string fullLoggerName, Dictionary<string, bool> loggersAdded, int start)
        {
            LoggerNode child;
            var firstPart = parts[start];
            if (!childrenDictionary.TryGetValue(firstPart, out child))
            {
                child = new LoggerNode
                {
                    Name = firstPart,
                    IsExpanded = true,
                    IsSelected = this.IsSelected
                };
                child.FullLoggerName = string.Join(".", parts, 0, start + 1);
                loggersAdded[child.FullLoggerName] = child.IsSelected;
                ChildLoggersList.Add(child);
                childrenDictionary.Add(firstPart, child);
                //ChildLoggers.Refresh();
            }
            if (start + 1 < parts.Length)
            {
                child.AddChild(parts, fullLoggerName, loggersAdded, start + 1);
            }
        }

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }
    }
}
