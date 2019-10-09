using System;

namespace LogReceiver

{
    public class MessageData
    {
        public DateTime TimeStamp { get; set; }

        public string Level { get; set; }

        public string Logger { get; set; }

        public string Message { get; set; }

        public string SingleLineMessage { get; set; }

        public static MessageData Parse(string input)
        {
            var parts = input.Split(new[] { '|' }, 4);
            var @event = new MessageData
            {
                TimeStamp = DateTime.Parse(parts[0]),
                Level = parts[1],
                Logger = parts[2],
                Message = parts[3],
                SingleLineMessage = parts[3].Replace("\n", "").Replace("\r", "")
            };
            return @event;
        }
    }
}
