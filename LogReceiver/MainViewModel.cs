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
                    searchText = value;
                    BeginInvokePropertyChanged(nameof(SearchText));
                    Events.IsLiveFiltering = !string.IsNullOrEmpty(value);
                    debouncedRefresh.Invoke();
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
                // Add to hierarchical tree
                loggerTreeBuilder.AddLogger(msg.Logger);
                
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

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
