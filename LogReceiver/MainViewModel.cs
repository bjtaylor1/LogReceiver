#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Events;
using ThrottleDebounce;

namespace LogReceiver
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly List<MessageData> eventList;
        
        private MessageData selectedMessage;
        private bool isPaused;

        public ICommand ClearCommand { get; }
        public ICommand TogglePauseCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ClearLoggerSearchCommand { get; }
        public ICommand DiagnosticsCommand { get; }

        private readonly LoggerTreeBuilder loggerTreeBuilder;
        private string searchText;
        private string loggerSearchText;
        public LoggerNodeModel LoggerTreeRoot => loggerTreeBuilder.RootNode;

        // Filtered tree view for performance
        private FilteredLoggerTreeViewModel _filteredTreeViewModel;
        public FilteredLoggerTreeViewModel FilteredTreeViewModel => _filteredTreeViewModel;

        // Level filtering
        private readonly HashSet<string> _allLevels = new HashSet<string>();
        private readonly HashSet<string> _selectedLevels = new HashSet<string>();
        private readonly object _levelsLock = new object();
        
        public IEnumerable<string> AllLevels
        {
            get
            {
                lock (_levelsLock)
                {
                    return _allLevels.OrderBy(l => l).ToList();
                }
            }
        }
        
        public HashSet<string> SelectedLevels => _selectedLevels;

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }));
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool IsPaused
        {
            get => isPaused; set
            {
                if (isPaused != value)
                {
                    isPaused = value;
                    BeginInvokePropertyChanged(nameof(IsPaused));
                    BeginInvokePropertyChanged(nameof(TogglePauseCommandContent));
                }
            }
        }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (searchText != value)
                {
                    var wasEmpty = string.IsNullOrEmpty(searchText);
                    var isEmpty = string.IsNullOrEmpty(value);
                    
                    searchText = value;
                    BeginInvokePropertyChanged(nameof(SearchText));
                    
                    // Only enable live filtering when we have search text
                    Events.IsLiveFiltering = !isEmpty;
                    
                    // If transitioning from empty to non-empty or vice versa, refresh immediately
                    if (wasEmpty != isEmpty)
                    {
                        Events.Refresh();
                    }
                    else if (!isEmpty)
                    {
                        // Use debounced refresh for search text changes
                        debouncedRefresh.Invoke();
                    }
                }
            }
        }

        public string LoggerSearchText
        {
            get => loggerSearchText;
            set
            {
                if (loggerSearchText != value)
                {
                    loggerSearchText = value;
                    BeginInvokePropertyChanged(nameof(LoggerSearchText));
                    _filteredTreeViewModel.FilterText = value;
                }
            }
        }

        private RateLimitedAction debouncedRefresh;
        private RateLimitedAction debouncedLoggerRefresh;

        public MessageData SelectedMessage
        {
            get { return selectedMessage; }
            set
            {
                if(selectedMessage != value)
                {
                    selectedMessage = value;
                    BeginInvokePropertyChanged(nameof(SelectedMessage));
                    BeginInvokePropertyChanged(nameof(FormattedSelectedMessage));
                }
            }
        }

        public string FormattedSelectedMessage
        {
            get
            {
                if (SelectedMessage?.Message == null)
                    return string.Empty;
                
                return $"{SelectedMessage.Message}\n\n{SelectedMessage.Exception}"
                    .Replace("\\r\\n", Environment.NewLine)
                    .Replace("\\n", Environment.NewLine)
                    .Replace("\\r", Environment.NewLine);
            }
        }

        public string TogglePauseCommandContent => IsPaused ? "Resume" : "Pause";

        public ListCollectionView Events { get; }
        private DispatcherTimer typeTimer = new DispatcherTimer();
        
        // Diagnostics tracking
        private DateTime lastMessageReceived = DateTime.MinValue;
        private int totalMessagesReceived = 0;
        private int messagesReceivedSinceLastDiagnostic = 0;
        
        public MainViewModel()
        {
            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList);
            loggerTreeBuilder = new LoggerTreeBuilder();
            _filteredTreeViewModel = new FilteredLoggerTreeViewModel(loggerTreeBuilder);
            
            // Subscribe to logger check state changes
            LoggerNodeModel.CheckStateChanged += OnLoggerCheckStateChanged;
            
            // Set up filtering for Events
            Events.Filter = FilterEvents;
            Events.LiveFilteringProperties.Add(nameof(MessageData.Logger));
            Events.LiveFilteringProperties.Add(nameof(MessageData.Message));
            Events.LiveFilteringProperties.Add(nameof(MessageData.Level));
            Events.IsLiveFiltering = false; // We'll enable this when needed
            
            ClearCommand = new DelegateCommand(Clear);
            TogglePauseCommand = new DelegateCommand(TogglePause);
            ClearSearchCommand = new DelegateCommand(ClearSearch);
            ClearLoggerSearchCommand = new DelegateCommand(ClearLoggerSearch);
            DiagnosticsCommand = new DelegateCommand(ShowDiagnostics);
            
            debouncedRefresh = Debouncer.Debounce(() => Application.Current.Dispatcher.Invoke(() =>
            {
                Events.Refresh();
            }), TimeSpan.FromSeconds(0.5));
            debouncedLoggerRefresh = Debouncer.Debounce(() => Application.Current.Dispatcher.Invoke(() =>
            {
                _filteredTreeViewModel.OnLoggerStateChanged();
            }), TimeSpan.FromSeconds(0.5));
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            Events.Refresh();
        }
        private void ClearLoggerSearch()
        {
            LoggerSearchText = string.Empty;
            _filteredTreeViewModel.FilterText = string.Empty;
        }

        private void TogglePause()
        {
            IsPaused = !IsPaused;
        }
        
        private void Clear()
        {
            eventList.Clear();
            
            // Clear level collections
            lock (_levelsLock)
            {
                _allLevels.Clear();
                _selectedLevels.Clear();
                BeginInvokePropertyChanged(nameof(AllLevels));
            }
            
            Events.Refresh();
            totalMessagesReceived = 0;
            messagesReceivedSinceLastDiagnostic = 0;
            lastMessageReceived = DateTime.MinValue;
            Console.WriteLine("MainViewModel: Event list cleared, counters reset");
        }

        private void ShowDiagnostics()
        {
            var now = DateTime.Now;
            var timeSinceLastMessage = lastMessageReceived == DateTime.MinValue ? 
                TimeSpan.MaxValue : now - lastMessageReceived;
            
            Console.WriteLine("=== DIAGNOSTICS ===");
            Console.WriteLine($"Current Time: {now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"IsPaused: {IsPaused}");
            Console.WriteLine($"Total Messages in List: {eventList.Count}");
            Console.WriteLine($"Total Messages Received: {totalMessagesReceived}");
            Console.WriteLine($"Messages Since Last Diagnostic: {messagesReceivedSinceLastDiagnostic}");
            Console.WriteLine($"Last Message Received: {(lastMessageReceived == DateTime.MinValue ? "NEVER" : lastMessageReceived.ToString("yyyy-MM-dd HH:mm:ss.fff"))}");
            Console.WriteLine($"Time Since Last Message: {(timeSinceLastMessage == TimeSpan.MaxValue ? "N/A" : timeSinceLastMessage.ToString(@"mm\:ss\.fff"))}");
            Console.WriteLine($"Events Collection Count: {Events.Count}");
            Console.WriteLine($"Events IsLiveFiltering: {Events.IsLiveFiltering}");
            Console.WriteLine($"Search Text: '{SearchText ?? "NULL"}'");
            Console.WriteLine($"Logger Search Text: '{LoggerSearchText ?? "NULL"}'");
            Console.WriteLine($"Logger Tree Root Children: {LoggerTreeRoot?.Children?.Count ?? 0}");
            
            // Check event aggregator subscription
            var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
            Console.WriteLine($"MessageEvent Subscribers: {messageEvent.GetType().GetField("subscriptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(messageEvent) != null}");
            
            Console.WriteLine("==================");
            
            // Reset diagnostic counter
            messagesReceivedSinceLastDiagnostic = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public void AddMessage(MessageData msg)
        {
            totalMessagesReceived++;
            messagesReceivedSinceLastDiagnostic++;
            lastMessageReceived = DateTime.Now;
            
            if (!IsPaused)
            {
                // Add level to the collection if it's new
                if (!string.IsNullOrWhiteSpace(msg.Level))
                {
                    lock (_levelsLock)
                    {
                        var wasNew = _allLevels.Add(msg.Level);
                        if (wasNew)
                        {
                            // New levels are selected by default
                            _selectedLevels.Add(msg.Level);
                            BeginInvokePropertyChanged(nameof(AllLevels));
                        }
                    }
                }
                
                // Add to hierarchical tree
                var wasNewLogger = loggerTreeBuilder.AddLogger(msg.Logger);
                
                // Notify filtered tree view if new logger was added
                if (wasNewLogger != null)
                {
                    _filteredTreeViewModel.OnLoggerAdded();
                }

                eventList.Insert(0, msg);

                if (eventList.Count > 5000)
                {
                    eventList.RemoveRange(3000, 2000);
                }
                
                // Always refresh since we have a filter applied
                // The filter will handle the logic of what to show/hide
                Events.Refresh();
            }
        }

        /// <summary>
        /// Filter predicate for Events collection - optimized for performance
        /// </summary>
        private bool FilterEvents(object item)
        {
            if (!(item is MessageData message))
            {
                return false;
            }

            // Check logger filtering first (most common filter)  
            // Use the tree builder's IsLoggerEnabled method which handles hierarchical logic
            if (!loggerTreeBuilder.IsLoggerEnabled(message.Logger))
            {
                return false;
            }

            // Check level filtering (AND logic with logger filtering)
            if (!string.IsNullOrWhiteSpace(message.Level))
            {
                lock (_levelsLock)
                {
                    if (!_selectedLevels.Contains(message.Level))
                    {
                        return false;
                    }
                }
            }

            // Check search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                var matchesSearch = message.Logger?.ToLowerInvariant().Contains(searchLower) == true ||
                       message.Message?.ToLowerInvariant().Contains(searchLower) == true ||
                       message.Level?.ToLowerInvariant().Contains(searchLower) == true;
                       
                return matchesSearch;
            }

            return true;
        }

        /// <summary>
        /// Handles level selection changes to update filtering
        /// </summary>
        public void OnLevelSelectionChanged(string level, bool isSelected)
        {
            lock (_levelsLock)
            {
                if (isSelected)
                {
                    _selectedLevels.Add(level);
                }
                else
                {
                    _selectedLevels.Remove(level);
                }
            }
            
            // Refresh the filtered events
            debouncedRefresh.Invoke();
        }

        /// <summary>
        /// <summary>
        /// Handles logger check state changes to update filtering
        /// </summary>
        private void OnLoggerCheckStateChanged(LoggerNodeModel node)
        {
            // Debounce the cache updates to avoid thrashing during recursive updates
            debouncedLoggerRefresh.Invoke();
            debouncedRefresh.Invoke();
        }

        public void Dispose()
        {
            LoggerNodeModel.CheckStateChanged -= OnLoggerCheckStateChanged;
        }
    }
}
