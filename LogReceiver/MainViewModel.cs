using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool defaultLoggerOption = true;

        public ICommand ClearCommand { get; }
        public ICommand TogglePauseCommand { get; }

        public ICommand ClearSearchCommand { get; }
        public ICommand ClearLoggerSearchCommand { get; }
        
        

        private readonly Dictionary<string, LoggerOption> loggerOptionsDictionary;
        private readonly List<LoggerOption> loggerOptionsList;
        private readonly LoggerTreeBuilder loggerTreeBuilder;
        private string searchText;
        private string loggerSearchText;
        private bool filterApplied = false;
        public ListCollectionView LoggerOptions { get; }
        public LoggerNodeModel LoggerTreeRoot => loggerTreeBuilder.RootNode;

        // Filtered tree view for performance
        private FilteredLoggerTreeViewModel _filteredTreeViewModel;
        public FilteredLoggerTreeViewModel FilteredTreeViewModel => _filteredTreeViewModel;

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }

        public bool DefaultLoggerOption
        {
            get => defaultLoggerOption;
            set
            {
                if (defaultLoggerOption != value)
                {
                    defaultLoggerOption = value;
                    BeginInvokePropertyChanged(nameof(DefaultLoggerOption));
                }
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
                    LoggerOptions.IsLiveFiltering = !string.IsNullOrEmpty(value);
                    debouncedLoggerRefresh.Invoke();
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
        
        public MainViewModel()
        {
            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList);
            loggerOptionsList = new List<LoggerOption>();
            LoggerOptions = new ListCollectionView(loggerOptionsList);
            loggerOptionsDictionary = new Dictionary<string, LoggerOption>();
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
            
            // Set up filtering for LoggerOptions  
            LoggerOptions.Filter = FilterLoggerOptions;
            LoggerOptions.LiveFilteringProperties.Add(nameof(LoggerOption.Logger));
            LoggerOptions.IsLiveFiltering = false; // We'll enable this when needed
            
            ClearCommand = new DelegateCommand(Clear);
            TogglePauseCommand = new DelegateCommand(TogglePause);
            AllOnCommand = new DelegateCommand(AllOn);
            AllOffCommand = new DelegateCommand(AllOff);
            GoToLoggerCommand = new DelegateCommand<string>(GoToLogger);
            OnlyLoggerCommand = new DelegateCommand<string>(OnlyLogger);
            ClearSearchCommand = new DelegateCommand(ClearSearch);
            ClearLoggerSearchCommand = new DelegateCommand(ClearLoggerSearch);
            
            debouncedRefresh = Debouncer.Debounce(() => Application.Current.Dispatcher.Invoke(() =>
            {
                Events.Refresh();
            }), TimeSpan.FromSeconds(0.5));
            debouncedLoggerRefresh = Debouncer.Debounce(() => Application.Current.Dispatcher.Invoke(() =>
            {
                LoggerOptions.Refresh();
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
            LoggerOptions.Refresh();
        }
        
        public DelegateCommand<string> GoToLoggerCommand { get; set; }
        public DelegateCommand<string> OnlyLoggerCommand { get; set; }

        public DelegateCommand AllOffCommand { get; set; }

        public DelegateCommand AllOnCommand { get; set; }

        private void AllOn()
        {
            foreach (var logger in loggerOptionsDictionary)
            {
                logger.Value.IsOn = true;
            }
        }

        private void AllOff()
        {
            foreach (var logger in loggerOptionsDictionary)
            {
                logger.Value.IsOn = false;
            }
        }

        private void GoToLogger(string logger)
        {
            var lastEventOfLogger = eventList.LastOrDefault(e => e.Logger == logger);
            if (lastEventOfLogger != null)
            {
                SelectedMessage = lastEventOfLogger;
            }
        }

        private void OnlyLogger(string logger)
        {
            using (Events.DeferRefresh())
            {
                using (LoggerOptions.DeferRefresh())
                {
                    foreach (var loggerOption in loggerOptionsList)
                    {
                        loggerOption.IsOn = string.Equals(loggerOption.Logger, logger);
                    }
                }
            }
        }

        private void TogglePause()
        {
            IsPaused = !IsPaused;
        }
        
        private void Clear()
        {
            eventList.Clear();
            Events.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public void AddMessage(MessageData msg)
        {
            if (!IsPaused)
            {
                // Add to hierarchical tree FIRST
                var wasNewLogger = loggerTreeBuilder.AddLogger(msg.Logger);
                
                // Notify filtered tree view if new logger was added
                if (wasNewLogger != null)
                {
                    _filteredTreeViewModel.OnLoggerAdded();
                }
                
                // Maintain compatibility with old system
                LoggerOption loggerOption;
                if (!loggerOptionsDictionary.TryGetValue(msg.Logger, out loggerOption))
                {
                    loggerOption = new LoggerOption(msg.Logger) { IsOn = DefaultLoggerOption };
                    loggerOption.PropertyChanged += HandleLoggerPropertyChanged;
                    loggerOptionsDictionary.Add(msg.Logger, loggerOption);
                    loggerOptionsList.Clear();
                    loggerOptionsList.AddRange(loggerOptionsDictionary.OrderBy(d => d.Key).Select(d => d.Value));
                    LoggerOptions.Refresh();
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

        private void HandleLoggerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoggerOption.IsOn))
            {
                debouncedRefresh.Invoke();
            }
        }

        /// <summary>
        /// Filter predicate for Events collection - optimized for performance
        /// </summary>
        private bool FilterEvents(object item)
        {
            if (!(item is MessageData message))
                return false;

            // Check logger filtering first (most common filter)  
            // Use the tree builder's IsLoggerEnabled method which handles hierarchical logic
            if (!loggerTreeBuilder.IsLoggerEnabled(message.Logger))
                return false;

            // Check search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                return message.Logger?.ToLowerInvariant().Contains(searchLower) == true ||
                       message.Message?.ToLowerInvariant().Contains(searchLower) == true ||
                       message.Level?.ToLowerInvariant().Contains(searchLower) == true;
            }

            return true;
        }

        /// <summary>
        /// Filter predicate for LoggerOptions collection
        /// </summary>
        private bool FilterLoggerOptions(object item)
        {
            if (!(item is LoggerOption loggerOption))
                return false;

            if (string.IsNullOrWhiteSpace(LoggerSearchText))
                return true;

            var searchLower = LoggerSearchText.ToLowerInvariant();
            return loggerOption.Logger?.ToLowerInvariant().Contains(searchLower) == true;
        }

        /// <summary>
        /// Handles logger check state changes to update filtering
        /// </summary>
        private void OnLoggerCheckStateChanged(LoggerNodeModel node)
        {
            _filteredTreeViewModel.OnLoggerStateChanged();
            
            // Refresh events when logger check states change
            debouncedRefresh.Invoke();
        }

        public void Dispose()
        {
            LoggerNodeModel.CheckStateChanged -= OnLoggerCheckStateChanged;
            // TODO release managed resources here
        }
    }
}
