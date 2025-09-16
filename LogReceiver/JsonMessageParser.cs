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
            Console.WriteLine("JsonMessageParser.ProcessAsync: Starting to process stream");
            int messageCount = 0;
            var startTime = DateTime.Now;
            
            using var textReader = new StreamReader(input);
            using var reader = new JsonTextReader(textReader) { SupportMultipleContent = true };
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"JsonMessageParser.ProcessAsync: Waiting for next message, processed {messageCount} so far");
                    
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        Console.WriteLine("JsonMessageParser.ProcessAsync: No more data to read, ending");
                        break;
                    }
                    
                    var serializer = new JsonSerializer
                    {
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    };
                    var data = serializer.Deserialize<T>(reader);
                    
                    messageCount++;
                    var elapsed = DateTime.Now - startTime;
                    Console.WriteLine($"JsonMessageParser.ProcessAsync: Deserialized message #{messageCount} after {elapsed.TotalSeconds:F2} seconds");
                    
                    messageReceived(data);
                    
                    if (messageCount % 50 == 0)
                    {
                        Console.WriteLine($"JsonMessageParser.ProcessAsync: Processed {messageCount} messages in {elapsed.TotalSeconds:F2} seconds");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"JsonMessageParser.ProcessAsync: Error reading from TCP stream: {e}");
                    break;
                }
            }

            Console.WriteLine($"JsonMessageParser.ProcessAsync: Finished processing stream, total messages: {messageCount}");
        }
    }
    
}
