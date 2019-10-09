using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Data;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : LoggerNode
    {
        public ItemAddedEvent itemAddedEvent { get; }

        private readonly List<MessageData> eventList;
        public ListCollectionView Events { get; }

        public MainViewModel(IEventAggregator eventAggregator) : base()
        {
            eventAggregator.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList);
        }

        public void AddLoggerRoot(string fullLoggerName)
        {
            AddChild(fullLoggerName.Split(new[] { '.' }), fullLoggerName);
        }

        private void AddMessage(MessageData msg)
        {
            eventList.Add(msg);
            AddLoggerRoot(msg.Logger);
            Events.Refresh();
        }
    }
}
