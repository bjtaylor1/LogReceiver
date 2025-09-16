#nullable disable
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
                Console.WriteLine($"Starting TCP Server on port: {port}");
                var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
                
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Console.WriteLine($"TCP Listener: Waiting for connection");
                    var tcpClient = await tcpListener.AcceptTcpClientAsync(cancellationTokenSource.Token);
                    Console.WriteLine($"TCP client accepted, handing it off to be processed {tcpClient.Client.Handle}");
                    _ = Task.Run(() => ReceiveMessageFromClient(tcpClient, messageEvent, cancellationTokenSource.Token));
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("TCP listener stopped");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal exception in TCP listener: {e}");
            }
            finally
            {
                tcpListener?.Stop();
                StoppedEvent.Set();
            }
        }

        private static async Task ReceiveMessageFromClient(TcpClient tcpClient, MessageEvent messageEvent, CancellationToken cancellationToken)
        {
            try
            {
                await using (var stream = tcpClient.GetStream())
                {
                    Console.WriteLine($"Reading message from {tcpClient.Client.Handle}");

                    int messageCount = 0;
                    await JsonMessageParser.ProcessAsync<MessageData>(stream, m =>
                    {
                        messageCount++;
                        Console.WriteLine($"Processed message #{messageCount} from {m.Logger}");
                        ProcessCompleteMessage(m, messageEvent);

                        // For stateless mode, we could throw an exception to stop after first message
                        // But let's see if NLog naturally sends one message per connection
                    }, cancellationTokenSource.Token).ConfigureAwait(false);

                    Console.WriteLine($"Processed {messageCount} messages, closing connection");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error in ReceiveMessageFromClient: {e}");
            }
            finally
            {
                var handle = tcpClient.Client.Handle;
                tcpClient.Close();
                Console.WriteLine($"Closed client {handle}");
            }
        }

        private static void ProcessCompleteMessage(MessageData messageData, MessageEvent messageEvent)
        {
            if (messageData == null)
            {
                Console.WriteLine("ProcessCompleteMessage: Received null message data");
                return;
            }
                
            try
            {
                if (!string.IsNullOrEmpty(messageData.Logger))
                {
                    Console.WriteLine($"ProcessCompleteMessage: Publishing message from logger '{messageData.Logger}', Level: {messageData.Level}");
                    messageEvent.Publish(messageData);
                }
                else
                {
                    Console.WriteLine("ProcessCompleteMessage: Received message with empty logger name, creating system error message");
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
                Console.WriteLine($"ProcessCompleteMessage: Error processing message: {e}");
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
                Console.WriteLine($"Error stopping TCP listener: {e}");
            }
            
            // Ensure the stopped event is always set
            StoppedEvent.Set();
        }
    }
}
