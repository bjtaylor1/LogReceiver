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
        private readonly Dictionary<string, bool> loggersTurnedOn = new Dictionary<string, bool>();
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
                }
            }
        }

        public MainViewModel() : base()
        {
            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            App.EventAggregator.Value.GetEvent<LoggerToggleEvent>().Subscribe(HandleToggleLoggersEvent, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList);// { Filter = FilterEvents };
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
            Settings.Default.AllLoggers = JsonConvert.SerializeObject(loggerModels, Formatting.Indented);
            Settings.Default.IncludedLoggersKvps = JsonConvert.SerializeObject(loggersTurnedOn.ToArray());
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
                }
                if (Settings.Default.IncludedLoggersKvps != null)
                {
                    var loggersTurnedOnPersisted = JsonConvert.DeserializeObject<KeyValuePair<string, bool>[]>(Settings.Default.IncludedLoggersKvps);
                    foreach (var loggerTurnedOn in loggersTurnedOnPersisted)
                    {
                        loggersTurnedOn[loggerTurnedOn.Key] = loggerTurnedOn.Value;
                    }

                }
                Events.Refresh();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to load existing data: " + e.Message);
            }
        }

        //private bool FilterEvents(object obj)
        //{
        //    var messageData = (MessageData)obj;
        //    if (loggersTurnedOn.TryGetValue(messageData.Logger, out bool setting))
        //        return setting;
        //    else
        //        return true;
        //}

        private bool IncludeLogger(string logger)
        {
            if (loggersTurnedOn.TryGetValue(logger, out bool setting))
                return setting;
            else
                return true;
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
                loggersTurnedOn[logger] = state;
                eventList.RemoveAll(md => md.Logger == logger);
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
            if (IncludeLogger(msg.Logger))
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
}
