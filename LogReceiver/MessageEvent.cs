using Prism.Events;

namespace LogReceiver

{
    public class MessageEvent : PubSubEvent<MessageData>
    {

    }

    public class ItemAddedEvent : PubSubEvent
    {

    }
}
