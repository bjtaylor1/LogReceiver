using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LogReceiver
{
    public static class LogListener
    {
        public static long Running = 0;
        internal static async Task Listen()
        {
            Debug.WriteLine("Starting Listen");
            var port = int.Parse(ConfigurationManager.AppSettings["port"]);
            var messageEvent = App.EventAggregator.Value.GetEvent<MessageEvent>();
            using (var udpClient = new UdpClient(port))
            {
                var endPoint = new IPEndPoint(IPAddress.Any, port);
                while (true)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();
                        var resultString = Encoding.UTF8.GetString(result.Buffer);
                        Debug.WriteLine(resultString);
                        var messageData = MessageData.Parse(resultString);
                        messageEvent.Publish(messageData);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }
    }
}
