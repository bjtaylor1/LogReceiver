using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace LogReceiver

{
    public class MessageData : INotifyPropertyChanged
    {
        private bool isHighlighted;

        [JsonProperty("time")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("logger")]
        public string Logger { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("exception")]
        public string Exception { get; set; }
        

        [JsonProperty("process")]
        public string Process { get; set; }

        private static readonly Regex FirstLineMatch = new(@"^([^\r\n]*)");
        public string SingleLineMessage => FirstLineMatch.Match(Message ?? "").Groups[1].Value;
        
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
    }
}
