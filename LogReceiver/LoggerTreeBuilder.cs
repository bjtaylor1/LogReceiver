using System.Collections.Generic;

namespace LogReceiver
{
    /// <summary>
    /// Builds and manages the hierarchical logger tree structure
    /// </summary>
    public class LoggerTreeBuilder
    {
        private readonly LoggerNodeModel _rootNode;
        private readonly Dictionary<string, LoggerNodeModel> _allNodes;
        private readonly object _lockObject = new object();

        public LoggerTreeBuilder()
        {
            _rootNode = new LoggerNodeModel
            {
                Name = "Root",
                FullLoggerName = "",
                CheckState = CheckState.Checked
            };
            _allNodes = new Dictionary<string, LoggerNodeModel>();
        }

        public LoggerNodeModel RootNode => _rootNode;

        /// <summary>
        /// Adds a logger to the tree, creating the necessary hierarchy
        /// </summary>
        /// <param name="loggerName">The full logger name (e.g., "BT.Debug.Log1")</param>
        /// <returns>The logger node, or null if the logger already existed</returns>
        public LoggerNodeModel AddLogger(string loggerName)
        {
            if (string.IsNullOrEmpty(loggerName))
                return null;

            lock (_lockObject)
            {
                if (_allNodes.TryGetValue(loggerName, out var existingNode))
                    return null; // Return null if logger already exists

                var parts = loggerName.Split('.');
                var currentNode = _rootNode;
                var currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}.{part}";

                    if (_allNodes.TryGetValue(currentPath, out var existingChild))
                    {
                        currentNode = existingChild;
                    }
                    else
                    {
                        var newNode = currentNode.FindOrCreateChild(part, currentPath);
                        
                        // New loggers always start as checked (enabled)
                        newNode.CheckState = CheckState.Checked;
                        
                        _allNodes[currentPath] = newNode;
                        currentNode = newNode;
                        
                        // Update parent states to reflect the new child
                        // This ensures parents become indeterminate if they have mixed children
                        newNode.Parent?.UpdateCheckStateFromChildren();
                    }
                }

                return currentNode;
            }
        }

        /// <summary>
        /// Gets all enabled logger names from the tree
        /// </summary>
        public HashSet<string> GetEnabledLoggers()
        {
            var enabledLoggers = new HashSet<string>();
            
            lock (_lockObject)
            {
                // Simply get all leaf nodes that are checked - the tree structure
                // already handles the hierarchical relationships correctly
                foreach (var node in _allNodes.Values)
                {
                    if (node.IsLeafNode && node.CheckState == CheckState.Checked)
                    {
                        enabledLoggers.Add(node.FullLoggerName);
                    }
                }
            }

            return enabledLoggers;
        }

        /// <summary>
        /// Checks if a logger should be visible based on the current tree state
        /// </summary>
        public bool IsLoggerEnabled(string loggerName)
        {
            if (string.IsNullOrEmpty(loggerName))
                return false;

            lock (_lockObject)
            {
                // Check if the exact logger exists and return its state
                if (_allNodes.TryGetValue(loggerName, out var node))
                {
                    return node.CheckState == CheckState.Checked;
                }

                // Defensive: This should not happen in normal operation since AddLogger()
                // is always called before IsLoggerEnabled(), but return true as fallback
                return true;
            }
        }
    }
}
