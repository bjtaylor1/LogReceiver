using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogReceiver

{
    public class Event
    {
        public DateTime TimeStamp { get; set; }

        public string Level { get; set; }

        public string Logger { get; set; }

        public string Message { get; set; }

        public static Event Parse(string input)
        {
            var parts = input.Split(new[] { '|' }, 4);
            var @event = new Event
            {
                TimeStamp = DateTime.Parse(parts[0]),
                Level = parts[1],
                Logger = parts[2],
                Message = parts[3]
            };
            return @event;
        }
    }
}
