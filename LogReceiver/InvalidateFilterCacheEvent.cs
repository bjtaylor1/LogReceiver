using Prism.Events;

namespace LogReceiver
{
    public class InvalidateFilterCacheEventArgs
    {
        public string[] AffectedLoggers { get; set; }
        public bool Value { get; set; }
    }

    public class InvalidateFilterCacheEvent : PubSubEvent<InvalidateFilterCacheEventArgs>
    {
    }
}
