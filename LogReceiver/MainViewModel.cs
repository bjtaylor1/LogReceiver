using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly List<MessageData> eventList;
        
        private MessageData selectedMessage;
        private bool isPaused;
        private bool defaultLoggerOption = true;

        public ICommand ClearCommand { get; }
        public ICommand ClearTreeCommand { get; }
        public ICommand TogglePauseCommand { get; }

        private readonly Dictionary<string, LoggerOption> loggerOptionsDictionary;
        private readonly List<LoggerOption> loggerOptionsList;
        public ListCollectionView LoggerOptions { get; }

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
        
        public MainViewModel()
        {
            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList);
            loggerOptionsList = new List<LoggerOption>();
            LoggerOptions = new ListCollectionView(loggerOptionsList);
            loggerOptionsDictionary = new Dictionary<string, LoggerOption>();
            ClearCommand = new DelegateCommand(Clear);
            TogglePauseCommand = new DelegateCommand(TogglePause);
            AllOnCommand = new DelegateCommand(AllOn);
            AllOffCommand = new DelegateCommand(AllOff);
            GoToLoggerCommand = new DelegateCommand<string>(GoToLogger);
        }

        public DelegateCommand<string> GoToLoggerCommand { get; set; }

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

        private void TogglePause()
        {
            IsPaused = !IsPaused;
        }
        
        private void Clear()
        {
            eventList.Clear();
            Events.Refresh();
        }

        private static readonly ConcurrentDictionary<string, bool> filterCache = new ConcurrentDictionary<string, bool>();
        public event PropertyChangedEventHandler PropertyChanged;


        public void AddMessage(MessageData msg)
        {
            if (!IsPaused)
            {
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

                if (loggerOption.IsOn)
                {
                    eventList.Add(msg);
                }

                if (eventList.Count > 5000)
                {
                    eventList.RemoveRange(0, 2000);
                }
                Events.Refresh();
            }
        }

        private void HandleLoggerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoggerOption.IsOn) && sender is LoggerOption loggerOption && !loggerOption.IsOn)
            {
                eventList.RemoveAll(@event => @event.Logger == loggerOption.Logger);
                Events.Refresh();
            }
        }
    }
}
