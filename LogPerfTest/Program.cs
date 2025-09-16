using NLog;
using System.Diagnostics;

namespace LogPerfTest;

class Program
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static volatile bool keepRunning = true;
    private static long totalMessagesSent = 0;
    private static readonly Stopwatch stopwatch = new Stopwatch();
    
    // Pre-generated stock messages for performance
    private static readonly string[] StockMessages = new[]
    {
        "Processing user authentication request with validation of credentials against multiple identity providers including active directory domain services and external oauth providers with token refresh capabilities",
        "Database connection pool exhausted during high load period resulting in queued operations with exponential backoff retry logic implementation across multiple connection attempts with circuit breaker pattern",
        "Memory allocation exceeded threshold limits triggering garbage collection cycle with full heap compaction and finalization queue processing affecting application response times temporarily",
        "Network timeout occurred while communicating with external service endpoint resulting in automatic failover to secondary datacenter infrastructure with load balancing redistribution",
        "Cache invalidation triggered by configuration change event propagating through distributed cache layers including redis cluster nodes and local memory caches with coherence protocols",
        "File system operation completed successfully after handling concurrent access conflicts with advisory locking mechanisms and atomic write operations ensuring data consistency",
        "Scheduled maintenance task executed background cleanup operations including log file rotation archive compression and temporary file removal across multiple storage volumes",
        "User session expired during extended idle period triggering automatic logout sequence with security audit logging and notification delivery to registered email addresses",
        "Payment processing workflow initiated with fraud detection analysis credit card validation merchant account verification and settlement transaction preparation",
        "Search indexing operation completed full text analysis with stemming lemmatization synonym expansion and relevance scoring calculation across document corpus"
    };
    
    // Logger name components for hierarchical naming (10x10x10 = ~1000 combinations)
    private static readonly string[] Level0Names = { "System", "Service", "Module", "Component", "Process", "Handler", "Manager", "Controller", "Provider", "Engine" };
    private static readonly string[] Level1Names = { "Auth", "Data", "Network", "Cache", "File", "Session", "Payment", "Search", "Config", "Monitor" };
    private static readonly string[] Level2Names = { "Core", "Handler", "Processor", "Validator", "Builder", "Factory", "Service", "Client", "Worker", "Manager" };
    
    private static readonly Random random = new Random();
    
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("LogPerfTest - Continuous NLog Performance Tester");
            Console.WriteLine("==============================================");
            Console.WriteLine("Press Ctrl+C to stop and show statistics");
            Console.WriteLine();
            
            // Parse command line arguments
            double messagesPerSecond = ParseRate(args);
            
            Console.WriteLine($"Sending messages at rate: {messagesPerSecond:F2} messages/second");
            Console.WriteLine($"Average interval: {(1000.0 / messagesPerSecond):F0} ms between messages");
            Console.WriteLine();
            
            // Set up Ctrl+C handler
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                keepRunning = false;
                Console.WriteLine("\nShutdown requested...");
            };
            
            // Start the message sending loop
            stopwatch.Start();
            SendMessagesLoop(messagesPerSecond);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
    
    static double ParseRate(string[] args)
    {
        if (args.Length == 0)
            return 3.0; // Default rate
            
        if (args.Length == 1 && double.TryParse(args[0], out double rate) && rate > 0)
            return rate;
            
        // Check for named parameter
        for (int i = 0; i < args.Length - 1; i++)
        {
            if ((args[i].Equals("-rate", StringComparison.OrdinalIgnoreCase) || 
                 args[i].Equals("--rate", StringComparison.OrdinalIgnoreCase)) &&
                double.TryParse(args[i + 1], out double namedRate) && namedRate > 0)
            {
                return namedRate;
            }
        }
        
        Console.WriteLine("Usage: LogPerfTest [rate] or LogPerfTest -rate [rate]");
        Console.WriteLine("  rate: Messages per second (default: 3.0, can be decimal like 0.5)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  LogPerfTest           (sends 3 messages/second)");
        Console.WriteLine("  LogPerfTest 5         (sends 5 messages/second)");
        Console.WriteLine("  LogPerfTest 0.5       (sends 1 message every 2 seconds)");
        Console.WriteLine("  LogPerfTest -rate 10  (sends 10 messages/second)");
        
        return 3.0; // Default if parsing fails
    }
    
    static void SendMessagesLoop(double messagesPerSecond)
    {
        double intervalMs = 1000.0 / messagesPerSecond;
        DateTime nextSendTime = DateTime.Now;
        
        Console.WriteLine("Starting message loop... Press Ctrl+C to stop");
        Console.WriteLine();
        
        while (keepRunning)
        {
            var currentTime = DateTime.Now;
            
            if (currentTime >= nextSendTime)
            {
                SendSingleMessage();
                
                // Schedule next message
                nextSendTime = nextSendTime.AddMilliseconds(intervalMs);
                
                // If we've fallen behind, reset to current time plus interval
                if (nextSendTime < currentTime)
                {
                    nextSendTime = currentTime.AddMilliseconds(intervalMs);
                }
            }
            
            // Small sleep to prevent busy waiting
            Thread.Sleep(1);
        }
        
        stopwatch.Stop();
        ShowStatistics();
    }
    
    static void SendSingleMessage()
    {
        try
        {
            var loggerName = GenerateLoggerName();
            var targetLogger = LogManager.GetLogger(loggerName);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var stockMessage = StockMessages[random.Next(StockMessages.Length)];
            var messageWithTimestamp = $"[SENT: {timestamp}] {stockMessage}";
            
            targetLogger.Info(messageWithTimestamp);
            
            var currentCount = Interlocked.Increment(ref totalMessagesSent);
            
            // Show progress every 100 messages
            if (currentCount % 100 == 0)
            {
                var elapsed = stopwatch.Elapsed;
                var avgRate = currentCount / elapsed.TotalSeconds;
                Console.WriteLine($"Messages sent: {currentCount:N0}, Elapsed: {elapsed:mm\\:ss}, Avg rate: {avgRate:F2}/sec");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    static string GenerateLoggerName() => $"BT.{GenerateLoggerNameRandom()}";
    static string GenerateLoggerNameRandom()
    {
        var level0 = Level0Names[random.Next(Level0Names.Length)];
        
        // 10% chance of single level logger
        if (random.Next(10) == 0)
            return level0;
            
        var level1 = Level1Names[random.Next(Level1Names.Length)];
        
        // 10% chance of two level logger
        if (random.Next(10) == 0)
            return $"{level0}.{level1}";
            
        // 80% chance of three level logger
        var level2 = Level2Names[random.Next(Level2Names.Length)];
        return $"{level0}.{level1}.{level2}";
    }
    
    static void ShowStatistics()
    {
        Console.WriteLine();
        Console.WriteLine("=== Final Statistics ===");
        Console.WriteLine($"Total messages sent: {totalMessagesSent:N0}");
        Console.WriteLine($"Total elapsed time: {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");
        
        if (stopwatch.Elapsed.TotalSeconds > 0)
        {
            var averageRate = totalMessagesSent / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Average rate: {averageRate:F2} messages/second");
        }
        
        Console.WriteLine($"Application stopped at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
    }
}
