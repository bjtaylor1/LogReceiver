using NLog;

namespace LogTest;

class Program
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    
    static void Main(string[] args)
    {
        try
        {
            // Check for help parameter
            if (args.Length > 0 && (args[0].Equals("-help", StringComparison.OrdinalIgnoreCase) || 
                                   args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                                   args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                                   args[0].Equals("/?", StringComparison.OrdinalIgnoreCase)))
            {
                ShowUsage();
                return;
            }
            
            Console.WriteLine("LogTest - NLog Test Message Sender");
            Console.WriteLine("==================================");
            
            // Parse command line arguments
            var parameters = ParseCommandLineArgs(args);
            
            // Required parameters
            var requiredParams = new[] { "logger", "message" };
            
            // Check for missing required parameters and prompt if needed
            foreach (var param in requiredParams)
            {
                if (!parameters.ContainsKey(param) || string.IsNullOrWhiteSpace(parameters[param]))
                {
                    parameters[param] = PromptForParameter(param);
                }
            }
            
            // Optional parameters with defaults
            if (!parameters.ContainsKey("level") || string.IsNullOrWhiteSpace(parameters["level"]))
            {
                parameters["level"] = "Info";
            }
            
            // Validate level
            if (!IsValidLogLevel(parameters["level"]))
            {
                Console.WriteLine($"Invalid log level '{parameters["level"]}'. Valid levels: Trace, Debug, Info, Warn, Error, Fatal");
                return;
            }
            
            // Send the log message
            SendLogMessage(parameters["logger"], parameters["level"], parameters["message"], parameters.GetValueOrDefault("exception"));
            
            Console.WriteLine();
            Console.WriteLine("Log message sent successfully!");
            Console.WriteLine($"Logger: {parameters["logger"]}");
            Console.WriteLine($"Level: {parameters["level"]}");
            Console.WriteLine($"Message: {parameters["message"]}");
            if (!string.IsNullOrWhiteSpace(parameters.GetValueOrDefault("exception")))
            {
                Console.WriteLine($"Exception: {parameters["exception"]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            ShowUsage();
            Environment.Exit(1);
        }
    }
    
    static Dictionary<string, string> ParseCommandLineArgs(string[] args)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("-") || args[i].StartsWith("/"))
            {
                var key = args[i][1..].ToLowerInvariant();
                
                // Handle --key=value format
                if (key.Contains('='))
                {
                    var parts = key.Split('=', 2);
                    parameters[parts[0]] = parts.Length > 1 ? parts[1] : "";
                }
                // Handle -key value format
                else if (i + 1 < args.Length && !args[i + 1].StartsWith('-') && !args[i + 1].StartsWith('/'))
                {
                    parameters[key] = args[i + 1];
                    i++; // Skip the next argument as it's the value
                }
                // Handle flags
                else
                {
                    parameters[key] = "true";
                }
            }
        }
        
        return parameters;
    }
    
    static string PromptForParameter(string paramName, string? defaultValue = null, bool isOptional = false)
    {
        string prompt = $"Enter {paramName}";
        if (!string.IsNullOrEmpty(defaultValue))
        {
            prompt += $" (default: {defaultValue})";
        }
        if (isOptional)
        {
            prompt += " (optional)";
        }
        prompt += ": ";
        
        Console.Write(prompt);
        string? value = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(defaultValue))
        {
            return defaultValue;
        }
        
        if (string.IsNullOrWhiteSpace(value) && !isOptional)
        {
            Console.WriteLine($"{paramName} is required!");
            return PromptForParameter(paramName, defaultValue, isOptional);
        }
        
        return value ?? string.Empty;
    }
    
    static bool IsValidLogLevel(string level)
    {
        var validLevels = new[] { "Trace", "Debug", "Info", "Warn", "Error", "Fatal" };
        return validLevels.Any(l => l.Equals(level, StringComparison.OrdinalIgnoreCase));
    }
    
    static void SendLogMessage(string loggerName, string level, string message, string? exception = null)
    {
        var targetLogger = LogManager.GetLogger(loggerName);
        var logLevel = LogLevel.FromString(level);
        
        if (string.IsNullOrWhiteSpace(exception))
        {
            targetLogger.Log(logLevel, message);
        }
        else
        {
            var ex = new Exception(exception);
            targetLogger.Log(logLevel, ex, message);
        }
    }
    
    static void ShowUsage()
    {
        Console.WriteLine("LogTest - NLog Test Message Sender");
        Console.WriteLine("==================================");
        Console.WriteLine();
        Console.WriteLine("Usage: LogTest [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -help, --help, -h, /?   Show this help message");
        Console.WriteLine();
        Console.WriteLine("Required Parameters:");
        Console.WriteLine("  -logger     Logger name (e.g., 'BT.Debug.Test' or 'MyApp.Service')");
        Console.WriteLine("  -message    Log message text");
        Console.WriteLine();
        Console.WriteLine("Optional Parameters:");
        Console.WriteLine("  -level      Log level (Trace, Debug, Info, Warn, Error, Fatal) [default: Info]");
        Console.WriteLine("  -exception  Exception text to include with the log message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  LogTest -logger BT.Debug.Test -message \"Test message\"");
        Console.WriteLine("  LogTest -logger BT.Debug.CommandInfo -message \"Command executed\" -level Debug");
        Console.WriteLine("  LogTest -logger MyApp.Service -message \"Service error\" -level Error -exception \"Connection failed\"");
        Console.WriteLine();
        Console.WriteLine("If required parameters are missing, you will be prompted to enter them interactively.");
    }
}
