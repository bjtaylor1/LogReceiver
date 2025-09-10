using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Handle JSON-encoded pipe-delimited format from NLog:
            // "${json-encode:${longdate}}|${json-encode:${level}}|${json-encode:${logger}}|${json-encode:${message}}${onexception:|${json-encode:${exception:format=tostring}}}"
            // This creates messages like: "\"2023-09-10 15:45:32.1234\"|\"INFO\"|\"MyLogger\"|\"This is the log message\""
            
            var parts = input.Split('|');
            
            // Handle JSON-encoded format: timestamp|level|logger|message|exception (exception is optional)
            if (parts.Length >= 4)
            {
                try
                {
                    // Decode JSON-encoded fields
                    var timestampJson = parts[0];
                    var levelJson = parts[1];
                    var loggerJson = parts[2];
                    
                    // Message could span multiple parts if it contains pipes
                    var messageStartIndex = 3;
                    var messageEndIndex = parts.Length - 1;
                    
                    // Check if the last part looks like an exception (starts with quote)
                    if (parts.Length > 4 && parts[parts.Length - 1].StartsWith("\""))
                    {
                        // Last part is likely an exception, so message ends before it
                        messageEndIndex = parts.Length - 2;
                    }
                    
                    var messageParts = new string[messageEndIndex - messageStartIndex + 1];
                    Array.Copy(parts, messageStartIndex, messageParts, 0, messageParts.Length);
                    var messageJson = string.Join("|", messageParts);
                    
                    // Parse JSON-encoded strings (remove quotes and handle escape sequences)
                    var timestamp = ParseJsonString(timestampJson);
                    var level = ParseJsonString(levelJson);
                    var logger = ParseJsonString(loggerJson);
                    var message = ParseJsonString(messageJson);
                    
                    // Add exception if present
                    if (parts.Length > 4 && messageEndIndex < parts.Length - 1)
                    {
                        var exceptionJson = parts[parts.Length - 1];
                        var exception = ParseJsonString(exceptionJson);
                        if (!string.IsNullOrEmpty(exception))
                        {
                            message = string.IsNullOrEmpty(message) ? exception : $"{message}\n{exception}";
                        }
                    }
                    
                    if (DateTime.TryParse(timestamp, out var parsedTimestamp))
                    {
                        var @event = new MessageData
                        {
                            TimeStamp = parsedTimestamp,
                            Level = level,
                            Logger = logger,
                            Message = message,
                            SingleLineMessage = message.Replace("\n", " ").Replace("\r", "")
                        };
                        
                        if (@event.SingleLineMessage.Length > 255)
                        {
                            @event.SingleLineMessage = @event.SingleLineMessage.Substring(0, 255);
                        }
                        
                        return @event;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error parsing JSON-encoded message: {e}");
                    // Fall through to legacy parsing
                }
            }
            
            // Fallback: Handle legacy pipe-delimited format for backward compatibility
            var pipeParts = input.Split('|');
            
            // Handle old format with sequence IDs: sequenceid|timestamp|level|logger|message|sequenceid
            if (pipeParts.Length >= 6 && 
                long.TryParse(pipeParts[0], out _) && 
                DateTime.TryParse(pipeParts[1], out var timestampNew))
            {
                var messageStartIndex = 4;
                var messageEndIndex = pipeParts.Length - 2;
                var messageParts = pipeParts.Skip(messageStartIndex).Take(messageEndIndex - messageStartIndex + 1);
                var message = string.Join("|", messageParts);
                
                var @event = new MessageData
                {
                    TimeStamp = timestampNew,
                    Level = pipeParts[2],
                    Logger = pipeParts[3],
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
            else if (pipeParts.Length >= 4 && DateTime.TryParse(pipeParts[0], out var timestamp))
            {
                var message = string.Join("|", pipeParts.Skip(3));
                
                var @event = new MessageData
                {
                    TimeStamp = timestamp,
                    Level = pipeParts[1],
                    Logger = pipeParts[2],
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

        private static string ParseJsonString(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return string.Empty;
                
            // Remove surrounding quotes if present
            var trimmed = jsonString.Trim();
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }
            
            // Decode common JSON escape sequences
            var result = new StringBuilder();
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (trimmed[i] == '\\' && i + 1 < trimmed.Length)
                {
                    switch (trimmed[i + 1])
                    {
                        case '\"':
                            result.Append('\"');
                            i++; // Skip the next character
                            break;
                        case '\\':
                            result.Append('\\');
                            i++; // Skip the next character
                            break;
                        case '/':
                            result.Append('/');
                            i++; // Skip the next character
                            break;
                        case 'n':
                            result.Append('\n');
                            i++; // Skip the next character
                            break;
                        case 'r':
                            result.Append('\r');
                            i++; // Skip the next character
                            break;
                        case 't':
                            result.Append('\t');
                            i++; // Skip the next character
                            break;
                        case 'b':
                            result.Append('\b');
                            i++; // Skip the next character
                            break;
                        case 'f':
                            result.Append('\f');
                            i++; // Skip the next character
                            break;
                        default:
                            // Unknown escape sequence, keep the backslash
                            result.Append(trimmed[i]);
                            break;
                    }
                }
                else
                {
                    result.Append(trimmed[i]);
                }
            }
            
            return result.ToString();
        }
    }
}
