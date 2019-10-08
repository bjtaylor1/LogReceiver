using System.Diagnostics;
using System.Windows.Data;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : Logger
    {
        public ListCollectionView Events { get; }

        public MainViewModel(IEventAggregator eventAggregator) : base()
        {
            eventAggregator.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
        }

        private void AddLoggerRoot(string fullLoggerName)
        {
            AddChild(fullLoggerName.Split(new[] { '.' }));
        }

        private void AddMessage(MessageData msg)
        {
            Debug.WriteLine("Received a message");
        }
    }
}
