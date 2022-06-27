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

        internal static async Task Listen()
        {
            try
            {
                Debug.WriteLine("Starting Listen");
                var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
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
                            if (messageData != null && !string.IsNullOrEmpty(messageData.Logger))
                            {
                                messageEvent.Publish(messageData);
                            }
                            else
                            {
                                messageEvent.Publish(new MessageData
                                {
                                    Level = "Error",
                                    Logger = "INVALID",
                                    Message = resultString,
                                    SingleLineMessage = "(garbled message received - investigate logger!)",
                                    TimeStamp = DateTime.Now

                                });
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
