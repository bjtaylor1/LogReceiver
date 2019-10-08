using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
            using (var udpClient = new UdpClient(port))
            {
                var endPoint = new IPEndPoint(IPAddress.Any, port);
                while (Interlocked.Read(ref Running) == 0)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();
                        var resultString = Encoding.UTF8.GetString(result.Buffer);
                        Console.WriteLine(resultString);
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
