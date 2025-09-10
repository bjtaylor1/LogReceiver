using System;
using System.ComponentModel;
using System.Diagnostics;
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
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Parse JSON format from NLog JsonLayout
            // Expected format: {"time":"2025-09-10T15:52:00.0000000Z","level":"INFO","logger":"BT.Debug","message":"this is the message\n","exception":"..."}
            try
            {
                var messageData = JsonConvert.DeserializeObject<MessageData>(input);
                if (messageData != null)
                {
                    // Combine message and exception if both exist
                    if (!string.IsNullOrEmpty(messageData.Exception))
                    {
                        var combinedMessage = string.IsNullOrEmpty(messageData.Message) 
                            ? messageData.Exception 
                            : $"{messageData.Message}\n{messageData.Exception}";
                        messageData.Message = combinedMessage;
                    }
                    
                    // Set SingleLineMessage for display
                    messageData.SingleLineMessage = (messageData.Message ?? "").Replace("\n", " ").Replace("\r", "");
                    if (messageData.SingleLineMessage.Length > 255)
                    {
                        messageData.SingleLineMessage = messageData.SingleLineMessage.Substring(0, 255);
                    }
                    
                    // Set defaults if needed
                    messageData.Level = messageData.Level ?? "INFO";
                    messageData.Logger = messageData.Logger ?? "Unknown";
                    messageData.Message = messageData.Message ?? "";
                    
                    return messageData;
                }
            }
            catch (JsonException e)
            {
                Debug.WriteLine($"Error parsing JSON message: {e}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error processing JSON message: {e}");
            }
            
            return null;
        }


    }
}
