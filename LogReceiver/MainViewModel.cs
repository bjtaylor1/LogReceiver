using System.Collections.Generic;
using System.Windows.Data;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : LoggerNode
    {
        private readonly List<MessageData> eventList;
        private readonly HashSet<string> loggersTurnedOn = new HashSet<string>();
        private MessageData selectedMessage;

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
            Events = new ListCollectionView(eventList) { Filter = FilterEvents };
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
