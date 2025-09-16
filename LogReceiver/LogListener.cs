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
                
                // Track connection count for diagnostics
                int connectionCount = 0;
                
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    connectionCount++;
                    Debug.WriteLine($"TCP Listener: Waiting for connection #{connectionCount}");
                    await HandleClientConnection(messageEvent, connectionCount);
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

        private static async Task HandleClientConnection(MessageEvent messageEvent, int connectionNumber)
        {
            TcpClient tcpClient = null;
            try
            {
                Debug.WriteLine($"Connection #{connectionNumber}: Waiting for TCP client connection...");
                
                // Wait for a client to connect with timeout
                var acceptTask = tcpListener.AcceptTcpClientAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationTokenSource.Token);
                var completedTask = await Task.WhenAny(acceptTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Debug.WriteLine($"Connection #{connectionNumber}: Timeout waiting for client connection");
                    return;
                }
                
                tcpClient = await acceptTask;
                Debug.WriteLine($"Connection #{connectionNumber}: TCP client connected from: {tcpClient.Client.RemoteEndPoint}");
                
                // Configure socket for better reliability
                tcpClient.ReceiveTimeout = 30000; // 30 seconds
                tcpClient.SendTimeout = 30000;    // 30 seconds
                
                using (tcpClient)
                using (var stream = tcpClient.GetStream())
                {
                    int messageCount = 0;
                    var lastMessageTime = DateTime.Now;
                    
                    await JsonMessageParser.ProcessAsync<MessageData>(stream, m => 
                    {
                        messageCount++;
                        lastMessageTime = DateTime.Now;
                        if (messageCount % 100 == 0)
                        {
                            Debug.WriteLine($"Connection #{connectionNumber}: Processed {messageCount} messages, last at {lastMessageTime:HH:mm:ss.fff}");
                        }
                        ProcessCompleteMessage(m, messageEvent);
                    }, cancellationTokenSource.Token).ConfigureAwait(false);
                    
                    Debug.WriteLine($"Connection #{connectionNumber}: Stream ended. Total messages processed: {messageCount}");
                }
                
                Debug.WriteLine($"Connection #{connectionNumber}: TCP client disconnected");
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine($"Connection #{connectionNumber}: TCP listener disposed");
                throw new OperationCanceledException(); // Convert to cancellation to break the outer loop
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Connection #{connectionNumber}: TCP listener cancelled");
                throw; // Re-throw to break the outer loop
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Connection #{connectionNumber}: TCP connection error: {e}");
                // Wait a bit before accepting the next connection
                await Task.Delay(1000, cancellationTokenSource.Token);
            }
            finally
            {
                tcpClient?.Close();
            }
        }

        private static void ProcessCompleteMessage(MessageData messageData, MessageEvent messageEvent)
        {
            if (messageData == null)
            {
                Debug.WriteLine("ProcessCompleteMessage: Received null message data");
                return;
            }
                
            try
            {
                if (!string.IsNullOrEmpty(messageData.Logger))
                {
                    Debug.WriteLine($"ProcessCompleteMessage: Publishing message from logger '{messageData.Logger}', Level: {messageData.Level}");
                    messageEvent.Publish(messageData);
                }
                else
                {
                    Debug.WriteLine("ProcessCompleteMessage: Received message with empty logger name, creating system error message");
                    messageEvent.Publish(new MessageData
                    {
                        Level = "ERROR",
                        Logger = "SYSTEM",
                        Message = messageData.Message ?? "(no message)",
                        TimeStamp = DateTime.Now
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"ProcessCompleteMessage: Error processing message: {e}");
                messageEvent.Publish(new MessageData
                {
                    Level = "ERROR",
                    Logger = "SYSTEM",
                    Message = $"Exception processing message: {e.Message}",
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
