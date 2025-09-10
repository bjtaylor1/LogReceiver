using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LogReceiver
{
    public static class LogListener
    {
        public static ManualResetEventSlim StoppedEvent = new ManualResetEventSlim();
        public static long Running = 0;
        private static readonly int port = int.Parse(ConfigurationManager.AppSettings["tcpPort"] ?? "4505");
        private static TcpListener tcpListener;
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        internal static async Task Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                Debug.WriteLine($"Starting TCP Server on port: {port}");
                var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
                
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await HandleClientConnection(messageEvent);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("TCP listener stopped");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Fatal exception in TCP listener: {e}");
            }
            finally
            {
                tcpListener?.Stop();
                StoppedEvent.Set();
            }
        }

        private static async Task HandleClientConnection(MessageEvent messageEvent)
        {
            try
            {
                Debug.WriteLine("Waiting for TCP client connection...");
                
                // Wait for a client to connect
                var tcpClient = await tcpListener.AcceptTcpClientAsync();
                Debug.WriteLine($"TCP client connected from: {tcpClient.Client.RemoteEndPoint}");
                
                using (tcpClient)
                using (var stream = tcpClient.GetStream())
                {
                    await ProcessClientMessages(stream, messageEvent);
                }
                
                Debug.WriteLine("TCP client disconnected");
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("TCP listener disposed");
                throw new OperationCanceledException(); // Convert to cancellation to break the outer loop
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("TCP listener cancelled");
                throw; // Re-throw to break the outer loop
            }
            catch (Exception e)
            {
                Debug.WriteLine($"TCP connection error: {e}");
                // Wait a bit before accepting the next connection
                await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }

        private static async Task ProcessClientMessages(NetworkStream stream, MessageEvent messageEvent)
        {
            // Use JsonMessageParser to handle JSON boundary detection
            var jsonParser = new JsonMessageParser();
            var buffer = new byte[4096];
            
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                    if (bytesRead == 0)
                        break; // Client disconnected
                    
                    // Create array with only the bytes we actually read
                    var actualData = new byte[bytesRead];
                    Array.Copy(buffer, actualData, bytesRead);
                    
                    // Process received bytes and extract complete JSON messages
                    var jsonMessages = jsonParser.ProcessBytes(actualData);
                    
                    // Process each complete message
                    foreach (var messageData in jsonMessages)
                    {
                        ProcessCompleteMessage(messageData, messageEvent);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error reading from TCP stream: {e}");
                    break;
                }
            }
        }



        private static void ProcessCompleteMessage(MessageData messageData, MessageEvent messageEvent)
        {
            if (messageData == null)
                return;
                
            try
            {
                if (!string.IsNullOrEmpty(messageData.Logger))
                {
                    messageEvent.Publish(messageData);
                }
                else
                {
                    messageEvent.Publish(new MessageData
                    {
                        Level = "ERROR",
                        Logger = "SYSTEM",
                        Message = messageData.Message ?? "(no message)",
                        SingleLineMessage = "(message with missing logger - investigate logger!)",
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
                    Message = $"Exception processing message: {e.Message}",
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
                tcpListener?.Stop();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error stopping TCP listener: {e}");
            }
            
            // Ensure the stopped event is always set
            StoppedEvent.Set();
        }
    }
}
