﻿using System;
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
                while (true)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();
                        var resultString = Encoding.UTF8.GetString(result.Buffer);

                        var messageData = MessageData.Parse(resultString);
                        if (messageData != null)
                        {
                            messageEvent.Publish(messageData);
                        }
                    }
                    catch (SocketException e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }
        }
    }
}
