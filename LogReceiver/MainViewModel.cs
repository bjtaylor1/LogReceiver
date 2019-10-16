﻿using System;
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
        private Dictionary<string, bool> loggersTurnedOn = new Dictionary<string, bool>();
        private MessageData selectedMessage;
        public ICommand ClearCommand { get; }
        public ICommand ClearTreeCommand { get; }

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
            foreach(var descendant in GetDescendantsAndSelf())
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
            App.EventAggregator.Value.GetEvent<LoggerToggleEvent>().Subscribe(HandleToggleLoggersEvent, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList) { Filter = FilterEvents };
            ClearCommand = new DelegateCommand(Clear);
            ClearTreeCommand = new DelegateCommand(ClearTree);
            Load();
        }

        private void Clear()
        {
            eventList.Clear();
            Events.Refresh();
        }

        private void ClearTree()
        {
            loggersTurnedOn.Clear();
            ClearNodes();
            ChildLoggers.Refresh();
        }

        internal void Save()
        {
            var loggerModels = Mapping.Mapper.Value.Map<List<LoggerNode>, List<LoggerNodeModel>>(ChildLoggersList);
            var loggers = JsonConvert.SerializeObject(loggerModels, Formatting.Indented);
            Settings.Default.AllLoggers = loggers;
            string loggersTurnedOnJson = JsonConvert.SerializeObject(loggersTurnedOn);
            Settings.Default.LoggerState = loggersTurnedOnJson;
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
                if (Settings.Default.LoggerState != null)
                {
                    loggersTurnedOn = JsonConvert.DeserializeObject<Dictionary<string, bool>>(Settings.Default.LoggerState) ?? new Dictionary<string, bool>();
                    // ok to reset this property, as it's not bound to
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
            if (loggersTurnedOn.TryGetValue(messageData.Logger, out var setting))
                return setting;
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
                if (logger != null)
                {
                    loggersTurnedOn[logger] = state;
                }
            }
        }

        public void AddLoggerRoot(string fullLoggerName)
        {
            var loggersAdded = new HashSet<string>();
            string[] parts = fullLoggerName.Split(new[] { '.' });
            AddChild(parts, fullLoggerName, loggersAdded, 0);
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

            Events.Filter = null;
            Events.Filter = FilterEvents;
            Events.Refresh();
        }
    }
}
