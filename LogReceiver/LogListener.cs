using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogReceiver
{
    public static class LogListener
    {
        public static ManualResetEventSlim StoppedEvent = new ManualResetEventSlim();
        public static long Running = 0;
        private static readonly string pipeName = ConfigurationManager.AppSettings["pipeName"] ?? "LogReceiverPipe";
        private static NamedPipeServerStream pipeServer;
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        internal static async Task Listen()
        {
            try
            {
                Debug.WriteLine($"Starting Named Pipe Server: {pipeName}");
                var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
                
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await HandleClientConnection(messageEvent);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Named pipe listener stopped");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Fatal exception in named pipe listener: {e}");
            }
            finally
            {
                StoppedEvent.Set();
            }
        }

        private static async Task HandleClientConnection(MessageEvent messageEvent)
        {
            try
            {
                // Create a new pipe server for each connection
                using (pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 
                       NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message))
                {
                    Debug.WriteLine("Waiting for client connection...");
                    
                    // Wait for a client to connect
                    await pipeServer.WaitForConnectionAsync(cancellationTokenSource.Token);
                    Debug.WriteLine("Client connected to named pipe");
                    
                    await ProcessClientMessages(messageEvent);
                    
                    Debug.WriteLine("Client disconnected from named pipe");
                }
            }
            catch (IOException e) when (e.Message.Contains("pipe is being closed"))
            {
                Debug.WriteLine("Pipe closed gracefully");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Named pipe listener cancelled");
                throw; // Re-throw to break the outer loop
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Named pipe error: {e}");
                // Wait a bit before trying to create a new pipe server
                await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }

        private static async Task ProcessClientMessages(MessageEvent messageEvent)
        {
            // Read complete messages from the connected client
            // NLog sends each log entry terminated with \0 delimiter
            var buffer = new byte[1];
            var messageBytes = new List<byte>();
            
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, 1, cancellationTokenSource.Token);
                    if (bytesRead == 0)
                        break; // Client disconnected
                    
                    ProcessReceivedByte(buffer[0], messageBytes, messageEvent);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error reading from pipe: {e}");
                    break;
                }
            }
        }

        private static void ProcessReceivedByte(byte receivedByte, List<byte> messageBytes, MessageEvent messageEvent)
        {
            if (receivedByte == 0) // Found message delimiter
            {
                if (messageBytes.Count > 0)
                {
                    // Convert accumulated bytes to string and process
                    string message = Encoding.UTF8.GetString(messageBytes.ToArray());
                    ProcessCompleteMessage(message.Trim(), messageEvent);
                    messageBytes.Clear();
                }
            }
            else
            {
                messageBytes.Add(receivedByte);
            }
        }

        private static void ProcessCompleteMessage(string message, MessageEvent messageEvent)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
                
            try
            {
                var messageData = MessageData.Parse(message);
                if (messageData != null && !string.IsNullOrEmpty(messageData.Logger))
                {
                    messageEvent.Publish(messageData);
                }
                else
                {
                    messageEvent.Publish(new MessageData
                    {
                        Level = "ERROR",
                        Logger = "SYSTEM",
                        Message = message,
                        SingleLineMessage = "(garbled message received - investigate logger!)",
                        TimeStamp = DateTime.Now
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error processing message: {e}");
                messageEvent.Publish(new MessageData
                {
                    Level = "ERROR",
                    Logger = "SYSTEM",
                    Message = $"Exception processing message: {e.Message}\nOriginal message: {message}",
                    SingleLineMessage = $"Exception processing message: {e.Message}",
                    TimeStamp = DateTime.Now
                });
            }
        }

        internal static void Stop()
        {
            cancellationTokenSource.Cancel();
            
            try
            {
                pipeServer?.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error disposing pipe server: {e}");
            }
        }
    }
}
