using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
        private static readonly int port = int.Parse(ConfigurationManager.AppSettings["port"]);
        private static readonly UdpClient udpClient = new UdpClient(port);
        private static readonly MessageEvent messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();

        internal static async Task Listen()
        {
            try
            {
                Debug.WriteLine("Starting Listen");
                var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
                var messageBuffer = new List<MessageData>();
                DateTime lastPublish = DateTime.MinValue;
                while (true)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();
                        var resultString = Encoding.UTF8.GetString(result.Buffer);
                        try
                        {
                            var messageData = MessageData.Parse(resultString);
                            messageBuffer.Add(messageData);
                            var now = DateTime.Now;
                            if(now.Subtract(lastPublish) > TimeSpan.FromMilliseconds(1000))
                            {
                                lastPublish = now;
                                Debug.WriteLine($"Publishing {messageBuffer.Count} messages");
                                messageEvent.Publish(messageBuffer.ToArray());
                                messageBuffer.Clear();
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                    catch (SocketException e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("Listener received fatal exception");
                StoppedEvent.Set();
            }
        }

        internal static void Stop()
        {
            udpClient.Dispose();
        }
    }
}
