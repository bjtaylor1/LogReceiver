using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LogReceiver
{
    public enum CheckState
    {
        Unchecked,
        Checked,
        Indeterminate
    }

    public class LoggerNodeModel : INotifyPropertyChanged
    {
        private CheckState _checkState = CheckState.Checked;
        private bool _isExpanded = false;
        private readonly ObservableCollection<LoggerNodeModel> _children = new ObservableCollection<LoggerNodeModel>();
        private readonly ReadOnlyObservableCollection<LoggerNodeModel> _readOnlyChildren;
        private bool _suppressEvents = false;

        public static event Action<LoggerNodeModel> CheckStateChanged;

        public LoggerNodeModel()
        {
            _readOnlyChildren = new ReadOnlyObservableCollection<LoggerNodeModel>(_children);
        }

        public string Name { get; set; }
        public string FullLoggerName { get; set; }
        public LoggerNodeModel Parent { get; set; }
        
        public CheckState CheckState
        {
            get => _checkState;
            set
            {
                if (_checkState != value)
                {
                    _checkState = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsChecked));
                    
                    // Notify about check state change for filtering (only if not suppressed)
                    if (!_suppressEvents)
                    {
                        CheckStateChanged?.Invoke(this);
                    }
                    
                    // Update children
                    if (value != CheckState.Indeterminate)
                    {
                        var isChecked = value == CheckState.Checked;
                        foreach (var child in Children)
                        {
                            child.SetIsCheckedRecursive(isChecked, updateParent: false);
                        }
                    }
                    
                    // Update parent
                    Parent?.UpdateCheckStateFromChildren();
                }
            }
        }

        public bool? IsChecked
        {
            get
            {
                switch (CheckState)
                {
                    case CheckState.Checked:
                        return true;
                    case CheckState.Unchecked:
                        return false;
                    case CheckState.Indeterminate:
                        return null;
                    default:
                        return false;
                }
            }
            set
            {
                if (value == true)
                    CheckState = CheckState.Checked;
                else if (value == false)
                    CheckState = CheckState.Unchecked;
                else
                    CheckState = CheckState.Indeterminate;
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReadOnlyObservableCollection<LoggerNodeModel> Children => _readOnlyChildren;

        public bool HasChildren => _children.Count > 0;

        public bool IsLeafNode => !HasChildren;

        /// <summary>
        /// Gets all leaf nodes (loggers without children) under this node
        /// </summary>
        public IEnumerable<LoggerNodeModel> GetLeafNodes()
        {
            if (IsLeafNode)
            {
                yield return this;
            }
            else
            {
                foreach (var child in Children)
                {
                    foreach (var leaf in child.GetLeafNodes())
                    {
                        yield return leaf;
                    }
                }
            }
        }

        private void SetIsCheckedRecursive(bool isChecked, bool updateParent = true)
        {
            // Suppress events during recursive updates to prevent event cascade
            _suppressEvents = true;
            try
            {
                CheckState = isChecked ? CheckState.Checked : CheckState.Unchecked;
                
                foreach (var child in Children)
                {
                    child.SetIsCheckedRecursive(isChecked, false);
                }
            }
            finally
            {
                _suppressEvents = false;
            }
            
            if (updateParent)
            {
                Parent?.UpdateCheckStateFromChildren();
            }
        }

        private void UpdateCheckStateFromChildren()
        {
            if (!HasChildren) return;

            var checkedCount = Children.Count(c => c.CheckState == CheckState.Checked);
            var uncheckedCount = Children.Count(c => c.CheckState == CheckState.Unchecked);
            var indeterminateCount = Children.Count(c => c.CheckState == CheckState.Indeterminate);

            CheckState newState;
            if (checkedCount == Children.Count)
            {
                newState = CheckState.Checked;
            }
            else if (uncheckedCount == Children.Count)
            {
                newState = CheckState.Unchecked;
            }
            else
            {
                newState = CheckState.Indeterminate;
            }

            if (_checkState != newState)
            {
                _checkState = newState;
                OnPropertyChanged(nameof(CheckState));
                OnPropertyChanged(nameof(IsChecked));
                
                // Only fire event if not suppressed
                if (!_suppressEvents)
                {
                    CheckStateChanged?.Invoke(this);
                }
                
                Parent?.UpdateCheckStateFromChildren();
            }
        }

        public LoggerNodeModel FindOrCreateChild(string name, string fullLoggerName)
        {
            var existing = Children.FirstOrDefault(c => c.Name == name);
            if (existing != null)
                return existing;

            var newChild = new LoggerNodeModel
            {
                Name = name,
                FullLoggerName = fullLoggerName,
                Parent = this
                // Note: CheckState will be set by the caller based on inheritance logic
            };

            _children.Add(newChild);  // Use private field to add to collection
            OnPropertyChanged(nameof(HasChildren));
            return newChild;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
