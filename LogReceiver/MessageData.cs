using System;
using System.ComponentModel;
using System.Diagnostics;

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
            var parts = input.Split(new[] { '|' }, 4);
            if (parts.Length == 4 && DateTime.TryParse(parts[0], out var timestamp))
            {
                var @event = new MessageData
                {
                    TimeStamp = timestamp,
                    Level = parts[1],
                    Logger = parts[2],
                    Message = parts[3],
                    SingleLineMessage = parts[3].Replace("\n", " ").Replace("\r", "")
                };
                if (@event.SingleLineMessage.Length > 255)
                {
                    @event.SingleLineMessage = @event.SingleLineMessage.Substring(0, 255);
                }
                return @event;
            }
            else return null;
        }
    }
}
