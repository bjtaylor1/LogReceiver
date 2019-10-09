﻿using Prism.Events;

namespace LogReceiver
{
    public class LoggerToggleEvent : PubSubEvent<LoggerToggleEventPayload>  
    {

    }

    public class LoggerToggleEventPayload
    {
        public string[] Loggers { get; set; }
        public bool Selected { get; set; }
    }
}