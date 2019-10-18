using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : LoggerNode
    {
        private readonly List<MessageData> eventList;
        private MessageData selectedMessage;
        private bool isPaused;

        public ICommand ClearCommand { get; }
        public ICommand ClearTreeCommand { get; }
        public ICommand TogglePauseCommand { get; }

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

        public string TogglePauseCommandContent => IsPaused ? "Resume" : "Pause";

        public ListCollectionView Events { get; }

        public MessageData SelectedMessage
        {
            get => selectedMessage; set
            {
                if (selectedMessage != value)
                {
                    selectedMessage = value;
                    BeginInvokePropertyChanged(nameof(SelectedMessage));
                    Highlight(selectedMessage?.Logger);
                }
            }
        }

        public void TreeViewSelect(LoggerNode node)
        {
            Highlight(node.FullLoggerName);
        }

        private void Highlight(string logger)
        {
            foreach (var @event in eventList)
            {
                @event.IsHighlighted = logger != null && (@event.Logger.Equals(logger) || @event.Logger.StartsWith($"{logger}."));
            }
            foreach (var descendant in GetDescendantsAndSelf())
            {
                descendant.IsHighlighted = logger != null && descendant.FullLoggerName != null &&
                    (descendant.FullLoggerName.Equals(logger) || logger.StartsWith($"{descendant.FullLoggerName}."));
            }
        }

        public MainViewModel() : base()
        {
            IsSelected = true;
            IsExpanded = true;

            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            App.EventAggregator.Value.GetEvent<RefreshListEvent>().Subscribe(RefreshList, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList) { Filter = FilterEvents };
            ClearCommand = new DelegateCommand(Clear);
            ClearTreeCommand = new DelegateCommand(ClearTree);
            TogglePauseCommand = new DelegateCommand(TogglePause);
            Load();
        }

        private void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        private void RefreshList()
        {
            Events.Refresh();
        }

        private void Clear()
        {
            eventList.Clear();
            Events.Refresh();
        }

        private void ClearTree()
        {
            ClearNodes();
            ChildLoggers.Refresh();
        }

        internal void Save()
        {
            var loggerModels = Mapping.Mapper.Value.Map<List<LoggerNode>, List<LoggerNodeModel>>(ChildLoggersList);
            var loggers = JsonConvert.SerializeObject(loggerModels, Formatting.Indented);
            Settings.Default.AllLoggers = loggers;
            Settings.Default.Save();
        }

        internal void Load()
        {
            try
            {
                var allLoggersJson = Settings.Default.AllLoggers;
                if (!string.IsNullOrEmpty(allLoggersJson))
                {
                    var allLoggerModels = JsonConvert.DeserializeObject<LoggerNodeModel[]>(allLoggersJson);
                    var allLoggers = Mapping.Mapper.Value.Map<LoggerNodeModel[], List<LoggerNode>>(allLoggerModels);
                    ChildLoggersList = allLoggers;
                    foreach (var logger in GetDescendantsAndSelf())
                    {
                        logger.ChildLoggers.Refresh();
                    }
                    ChildLoggers.Refresh();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to load existing data: " + e.Message);
            }
        }

        public bool FilterEvents(object obj)
        {
            var messageData = (MessageData)obj;
            var parts = messageData.Logger.Split('.');
            var include = IsTurnedOn(parts, 0);
            return include;
        }

        public void AddLoggerRoot(string fullLoggerName)
        {
            var loggersAdded = new Dictionary<string, bool>();
            string[] parts = fullLoggerName.Split(new[] { '.' });
            AddChild(parts, fullLoggerName, loggersAdded, 0);
        }

        private void AddMessage(MessageData msg)
        {
            if (!IsPaused)
            {
                eventList.Add(msg);
                AddLoggerRoot(msg.Logger);

                if (eventList.Count > 25000)
                {
                    eventList.RemoveRange(0, 5000);
                }

                Events.Refresh();
            }
        }
    }
}
