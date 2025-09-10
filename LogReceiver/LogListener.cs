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
            // Read complete messages from the connected client
            // Messages are terminated with null byte (\0) to handle multi-line log entries
            var buffer = new byte[1];
            var messageBytes = new List<byte>();
            
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, 1, cancellationTokenSource.Token);
                    if (bytesRead == 0)
                        break; // Client disconnected
                    
                    ProcessReceivedByte(buffer[0], messageBytes, messageEvent);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error reading from TCP stream: {e}");
                    break;
                }
            }
        }

        private static void ProcessReceivedByte(byte receivedByte, List<byte> messageBytes, MessageEvent messageEvent)
        {
            if (receivedByte == 0) // Found null byte delimiter (\0)
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
