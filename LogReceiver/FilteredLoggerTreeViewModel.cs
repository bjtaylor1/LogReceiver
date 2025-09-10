using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LogReceiver
{
    /// <summary>
    /// High-performance filtered view of the logger tree that minimizes re-evaluation
    /// </summary>
    public class FilteredLoggerTreeViewModel : INotifyPropertyChanged
    {
        private readonly LoggerTreeBuilder _treeBuilder;
        private readonly ObservableCollection<LoggerNodeModel> _filteredItems;
        private string _filterText;
        private readonly Dictionary<string, bool> _filterCache;
        private readonly HashSet<string> _enabledLoggers;

        public FilteredLoggerTreeViewModel(LoggerTreeBuilder treeBuilder)
        {
            _treeBuilder = treeBuilder;
            _filteredItems = new ObservableCollection<LoggerNodeModel>();
            _filterCache = new Dictionary<string, bool>();
            _enabledLoggers = new HashSet<string>();
            RefreshFilteredItems();
        }

        public ObservableCollection<LoggerNodeModel> FilteredItems => _filteredItems;

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText != value)
                {
                    _filterText = value;
                    OnPropertyChanged();
                    InvalidateFilter();
                    RefreshFilteredItems();
                }
            }
        }

        public HashSet<string> EnabledLoggers => _enabledLoggers;

        /// <summary>
        /// Invalidates the filter cache when the filter criteria changes
        /// </summary>
        private void InvalidateFilter()
        {
            _filterCache.Clear();
        }

        /// <summary>
        /// Refreshes the filtered items collection
        /// </summary>
        public void RefreshFilteredItems()
        {
            _filteredItems.Clear();
            
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                // No filter - show all root children
                foreach (var child in _treeBuilder.RootNode.Children)
                {
                    _filteredItems.Add(child);
                }
            }
            else
            {
                // Apply filter - show only matching nodes and their ancestors
                var matchingNodes = GetFilteredNodes(_filterText.ToLowerInvariant());
                var nodesToShow = new HashSet<LoggerNodeModel>();
                
                // Add matching nodes and their ancestors
                foreach (var node in matchingNodes)
                {
                    AddNodeAndAncestors(node, nodesToShow);
                }
                
                // Add to collection in hierarchical order
                foreach (var child in _treeBuilder.RootNode.Children)
                {
                    if (ShouldShowNodeInHierarchy(child, nodesToShow))
                    {
                        _filteredItems.Add(CreateFilteredNode(child, nodesToShow));
                    }
                }
            }
            
            UpdateEnabledLoggers();
        }

        /// <summary>
        /// Gets all nodes that match the filter text
        /// </summary>
        private IEnumerable<LoggerNodeModel> GetFilteredNodes(string filterText)
        {
            return _treeBuilder.RootNode.Children
                .SelectMany(GetAllDescendants)
                .Where(node => NodeMatchesFilter(node, filterText));
        }

        /// <summary>
        /// Checks if a node matches the filter (with caching)
        /// </summary>
        private bool NodeMatchesFilter(LoggerNodeModel node, string filterText)
        {
            var cacheKey = $"{node.FullLoggerName}|{filterText}";
            
            if (!_filterCache.TryGetValue(cacheKey, out var matches))
            {
                matches = node.Name.ToLowerInvariant().Contains(filterText) ||
                         node.FullLoggerName.ToLowerInvariant().Contains(filterText);
                _filterCache[cacheKey] = matches;
            }
            
            return matches;
        }

        /// <summary>
        /// Gets all descendants of a node
        /// </summary>
        private IEnumerable<LoggerNodeModel> GetAllDescendants(LoggerNodeModel node)
        {
            yield return node;
            
            foreach (var child in node.Children)
            {
                foreach (var descendant in GetAllDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Adds a node and all its ancestors to the set
        /// </summary>
        private void AddNodeAndAncestors(LoggerNodeModel node, HashSet<LoggerNodeModel> nodesToShow)
        {
            var current = node;
            while (current != null && current != _treeBuilder.RootNode)
            {
                nodesToShow.Add(current);
                current = current.Parent;
            }
        }

        /// <summary>
        /// Checks if a node should be shown in the hierarchy
        /// </summary>
        private bool ShouldShowNodeInHierarchy(LoggerNodeModel node, HashSet<LoggerNodeModel> nodesToShow)
        {
            if (string.IsNullOrWhiteSpace(_filterText))
                return true;
                
            return nodesToShow.Contains(node) || 
                   node.Children.Any(child => ShouldShowNodeInHierarchy(child, nodesToShow));
        }

        /// <summary>
        /// Creates a filtered version of a node (only showing relevant children)
        /// </summary>
        private LoggerNodeModel CreateFilteredNode(LoggerNodeModel originalNode, HashSet<LoggerNodeModel> nodesToShow)
        {
            if (string.IsNullOrWhiteSpace(_filterText))
                return originalNode;

            // For filtering, we return the original node but its Children collection
            // will be filtered by the TreeView template binding
            return originalNode;
        }

        /// <summary>
        /// Updates the enabled loggers set based on current tree state
        /// </summary>
        private void UpdateEnabledLoggers()
        {
            _enabledLoggers.Clear();
            var enabledLoggers = _treeBuilder.GetEnabledLoggers();
            foreach (var logger in enabledLoggers)
            {
                _enabledLoggers.Add(logger);
            }
        }

        /// <summary>
        /// Call this when a new logger is added to refresh the view
        /// </summary>
        public void OnLoggerAdded()
        {
            InvalidateFilter();
            RefreshFilteredItems();
        }

        /// <summary>
        /// Call this when logger check states change
        /// </summary>
        public void OnLoggerStateChanged()
        {
            UpdateEnabledLoggers();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
