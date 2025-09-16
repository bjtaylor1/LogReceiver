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

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
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
                }
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
            Events.Refresh();
            totalMessagesReceived = 0;
            messagesReceivedSinceLastDiagnostic = 0;
            lastMessageReceived = DateTime.MinValue;
            Debug.WriteLine("MainViewModel: Event list cleared, counters reset");
        }

        private void ShowDiagnostics()
        {
            var now = DateTime.Now;
            var timeSinceLastMessage = lastMessageReceived == DateTime.MinValue ? 
                TimeSpan.MaxValue : now - lastMessageReceived;
            
            Debug.WriteLine("=== DIAGNOSTICS ===");
            Debug.WriteLine($"Current Time: {now:yyyy-MM-dd HH:mm:ss.fff}");
            Debug.WriteLine($"IsPaused: {IsPaused}");
            Debug.WriteLine($"Total Messages in List: {eventList.Count}");
            Debug.WriteLine($"Total Messages Received: {totalMessagesReceived}");
            Debug.WriteLine($"Messages Since Last Diagnostic: {messagesReceivedSinceLastDiagnostic}");
            Debug.WriteLine($"Last Message Received: {(lastMessageReceived == DateTime.MinValue ? "NEVER" : lastMessageReceived.ToString("yyyy-MM-dd HH:mm:ss.fff"))}");
            Debug.WriteLine($"Time Since Last Message: {(timeSinceLastMessage == TimeSpan.MaxValue ? "N/A" : timeSinceLastMessage.ToString(@"mm\:ss\.fff"))}");
            Debug.WriteLine($"Events Collection Count: {Events.Count}");
            Debug.WriteLine($"Events IsLiveFiltering: {Events.IsLiveFiltering}");
            Debug.WriteLine($"Search Text: '{SearchText ?? "NULL"}'");
            Debug.WriteLine($"Logger Search Text: '{LoggerSearchText ?? "NULL"}'");
            Debug.WriteLine($"Logger Tree Root Children: {LoggerTreeRoot?.Children?.Count ?? 0}");
            
            // Check event aggregator subscription
            var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
            Debug.WriteLine($"MessageEvent Subscribers: {messageEvent.GetType().GetField("subscriptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(messageEvent) != null}");
            
            Debug.WriteLine("==================");
            
            // Reset diagnostic counter
            messagesReceivedSinceLastDiagnostic = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public void AddMessage(MessageData msg)
        {
            totalMessagesReceived++;
            messagesReceivedSinceLastDiagnostic++;
            lastMessageReceived = DateTime.Now;
            
            Debug.WriteLine($"MainViewModel.AddMessage: Received message #{totalMessagesReceived} from '{msg.Logger}' at {lastMessageReceived:HH:mm:ss.fff}, IsPaused: {IsPaused}");
            
            if (!IsPaused)
            {
                // Add to hierarchical tree
                var wasNewLogger = loggerTreeBuilder.AddLogger(msg.Logger);
                
                // Notify filtered tree view if new logger was added
                if (wasNewLogger != null)
                {
                    Debug.WriteLine($"MainViewModel.AddMessage: New logger added: {wasNewLogger.FullLoggerName}");
                    _filteredTreeViewModel.OnLoggerAdded();
                }

                eventList.Insert(0, msg);
                Debug.WriteLine($"MainViewModel.AddMessage: Added to eventList, new count: {eventList.Count}");

                if (eventList.Count > 5000)
                {
                    eventList.RemoveRange(3000, 2000);
                    Debug.WriteLine("MainViewModel.AddMessage: Trimmed eventList from 5000 to 3000 items");
                }
                
                // Always refresh since we have a filter applied
                // The filter will handle the logic of what to show/hide
                Events.Refresh();
                Debug.WriteLine($"MainViewModel.AddMessage: Called Events.Refresh(), Events.Count: {Events.Count}");
            }
            else
            {
                Debug.WriteLine("MainViewModel.AddMessage: Skipped processing because IsPaused is true");
            }
        }

        /// <summary>
        /// Filter predicate for Events collection - optimized for performance
        /// </summary>
        private bool FilterEvents(object item)
        {
            if (!(item is MessageData message))
            {
                Debug.WriteLine("FilterEvents: Item is not MessageData, filtering out");
                return false;
            }

            // Check logger filtering first (most common filter)  
            // Use the tree builder's IsLoggerEnabled method which handles hierarchical logic
            if (!loggerTreeBuilder.IsLoggerEnabled(message.Logger))
            {
                Debug.WriteLine($"FilterEvents: Logger '{message.Logger}' is disabled, filtering out");
                return false;
            }

            // Check search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                var matchesSearch = message.Logger?.ToLowerInvariant().Contains(searchLower) == true ||
                       message.Message?.ToLowerInvariant().Contains(searchLower) == true ||
                       message.Level?.ToLowerInvariant().Contains(searchLower) == true;
                       
                if (!matchesSearch)
                {
                    Debug.WriteLine($"FilterEvents: Message from '{message.Logger}' doesn't match search '{SearchText}', filtering out");
                }
                
                return matchesSearch;
            }

            Debug.WriteLine($"FilterEvents: Message from '{message.Logger}' passed all filters");
            return true;
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
