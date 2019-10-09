using System.Diagnostics;
using System.Windows.Data;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : LoggerNode
    {
        public ListCollectionView Events { get; }

        public MainViewModel(IEventAggregator eventAggregator) : base()
        {
            eventAggregator.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
        }

        public void AddLoggerRoot(string fullLoggerName)
        {
            Debug.WriteLine($"Adding logger {fullLoggerName}");

            AddChild(fullLoggerName.Split(new[] { '.' }), fullLoggerName);

        }

        private void AddMessage(MessageData msg)
        {
            AddLoggerRoot(msg.Logger);
        }
    }
}
