using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        private readonly HashSet<string> loggersTurnedOn = new HashSet<string>();
        private MessageData selectedMessage;
        public ICommand ClearCommand { get; }

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
            foreach(var @event in eventList)
            {
                @event.IsHighlighted = logger != null && (@event.Logger.Equals(logger) || @event.Logger.StartsWith($"{logger}."));
            }
        }

        public MainViewModel() : base()
        {
            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            App.EventAggregator.Value.GetEvent<LoggerToggleEvent>().Subscribe(HandleToggleLoggersEvent, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList) { Filter = FilterEvents };
            ClearCommand = new DelegateCommand(Clear);
            Load();
        }

        private void Clear()
        {
            eventList.Clear();
            Events.Refresh();
        }

        internal void Save()
        {
            var loggerModels = Mapping.Mapper.Value.Map<List<LoggerNode>, List<LoggerNodeModel>>(ChildLoggersList);
            var loggers = JsonConvert.SerializeObject(loggerModels, Formatting.Indented);
            Settings.Default.AllLoggers = loggers;
            Settings.Default.IncludedLoggers = new StringCollection();
            Settings.Default.IncludedLoggers.AddRange(loggersTurnedOn.ToArray());
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
                if (Settings.Default.IncludedLoggers != null)
                {
                    foreach (var loggerTurnedOn in Settings.Default.IncludedLoggers)
                    {
                        loggersTurnedOn.Add(loggerTurnedOn);
                    }
                    Events.Refresh();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to load existing data: " + e.Message);
            }
        }

        private bool FilterEvents(object obj)
        {
            var messageData = (MessageData)obj;
            var include = loggersTurnedOn.Contains(messageData.Logger);
            return include;
        }

        private void HandleToggleLoggersEvent(LoggerToggleEventPayload payload)
        {
            ToggleLoggers(payload.Loggers, payload.Selected);
            Events.Refresh();
        }

        private void ToggleLoggers(IEnumerable<string> loggers, bool state)
        {
            foreach (var logger in loggers)
            {
                if (state)
                    loggersTurnedOn.Add(logger);
                else
                    loggersTurnedOn.Remove(logger);
            }
        }

        public void AddLoggerRoot(string fullLoggerName)
        {
            var loggersAdded = new HashSet<string>();
            AddChild(fullLoggerName.Split(new[] { '.' }), fullLoggerName, loggersAdded);
            ToggleLoggers(loggersAdded, true);
        }

        private void AddMessage(MessageData msg)
        {
            eventList.Add(msg);
            AddLoggerRoot(msg.Logger);

            if (eventList.Count > 5000)
            {
                eventList.RemoveRange(0, 2000);
            }

            Events.Refresh();
        }
    }
}
