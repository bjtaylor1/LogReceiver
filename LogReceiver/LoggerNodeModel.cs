using System.Collections.Generic;

namespace LogReceiver

{
    public class LoggerNodeModel
    {
        public string Name { get; set; }
        public string FullLoggerName { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public List<LoggerNodeModel> ChildLoggersList { get; set; }
    }
}
