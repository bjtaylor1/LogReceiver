using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace LogReceiver

{
    public class MessageData : INotifyPropertyChanged
    {
        private bool isHighlighted;

        public DateTime TimeStamp { get; set; }

        public string Level { get; set; }

        public string Logger { get; set; }

        public string Message { get; set; }

        public string SingleLineMessage { get; set; }
        public bool IsHighlighted
        {
            get => isHighlighted;
            set
            {
                if (isHighlighted != value)
                {
                    isHighlighted = value;
                    NotifyPropertyChanged(nameof(IsHighlighted));
                }
            }
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static MessageData Parse(string input)
        {
            var parts = input.Split('|');
            
            // Handle new format with sequence IDs: sequenceid|timestamp|level|logger|message|sequenceid
            if (parts.Length >= 6 && 
                long.TryParse(parts[0], out _) && 
                DateTime.TryParse(parts[1], out var timestampNew))
            {
                // Extract message content (everything between logger and final sequence ID)
                var messageStartIndex = 4;
                var messageEndIndex = parts.Length - 2;
                var messageParts = parts.Skip(messageStartIndex).Take(messageEndIndex - messageStartIndex + 1);
                var message = string.Join("|", messageParts);
                
                var @event = new MessageData
                {
                    TimeStamp = timestampNew,
                    Level = parts[2],
                    Logger = parts[3],
                    Message = message,
                    SingleLineMessage = message.Replace("\n", " ").Replace("\r", "")
                };
                if (@event.SingleLineMessage.Length > 255)
                {
                    @event.SingleLineMessage = @event.SingleLineMessage.Substring(0, 255);
                }
                return @event;
            }
            
            // Handle legacy format: timestamp|level|logger|message
            else if (parts.Length >= 4 && DateTime.TryParse(parts[0], out var timestamp))
            {
                // Extract message content (everything from index 3 onwards)
                var message = string.Join("|", parts.Skip(3));
                
                var @event = new MessageData
                {
                    TimeStamp = timestamp,
                    Level = parts[1],
                    Logger = parts[2],
                    Message = message,
                    SingleLineMessage = message.Replace("\n", " ").Replace("\r", "")
                };
                if (@event.SingleLineMessage.Length > 255)
                {
                    @event.SingleLineMessage = @event.SingleLineMessage.Substring(0, 255);
                }
                return @event;
            }
            
            return null;
        }
    }
}
