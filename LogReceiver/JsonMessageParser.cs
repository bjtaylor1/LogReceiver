using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LogReceiver
{
    public static class JsonMessageParser
    {
        public static async Task ProcessAsync<T>(Stream input, Action<T> messageReceived, CancellationToken cancellationToken)
        {
            using var textReader = new StreamReader(input);
            using var reader = new JsonTextReader(textReader) { SupportMultipleContent = true };
            
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        break;
                    }
                    var serializer = new JsonSerializer
                    {
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    };
                    var data = serializer.Deserialize<T>(reader);
                    messageReceived(data);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error reading from TCP stream: {e}");
                    break;
                }
            }

        }
    }
    
}
