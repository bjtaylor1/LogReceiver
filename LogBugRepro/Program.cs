using NLog;
using System;
using System.Threading;

namespace LogBugRepro
{
    class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static volatile bool keepRunning = true;
        private static int messageCount = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("LogBugRepro - TCP Connection Bug Reproduction Tool");
            Console.WriteLine("This tool will:");
            Console.WriteLine("1. Send some messages to LogReceiver");
            Console.WriteLine("2. Abruptly terminate the TCP connection");
            Console.WriteLine("3. Leave LogReceiver waiting indefinitely");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C at any time to exit gracefully");
            Console.WriteLine("=" + new string('=', 50));

            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                keepRunning = false;
                Console.WriteLine("\nShutting down gracefully...");
            };

            try
            {
                // Phase 1: Send messages normally
                Console.WriteLine("Phase 1: Sending messages to establish connection...");
                for (int i = 1; i <= 10 && keepRunning; i++)
                {
                    logger.Info($"Setup message #{i} - establishing TCP connection");
                    messageCount++;
                    Thread.Sleep(200); // 200ms between messages
                    
                    if (i % 5 == 0)
                    {
                        Console.WriteLine($"  Sent {i} setup messages");
                    }
                }

                if (!keepRunning) return;

                Console.WriteLine("Phase 2: Sending a few more messages...");
                for (int i = 1; i <= 5 && keepRunning; i++)
                {
                    logger.Warn($"Pre-disconnect message #{i} - connection active");
                    messageCount++;
                    Thread.Sleep(300);
                }

                if (!keepRunning) return;

                Console.WriteLine("\nPhase 3: FORCING TCP CONNECTION TERMINATION");
                Console.WriteLine("This will cause LogReceiver to wait for a new connection...");
                
                // Force NLog to flush and close connections
                LogManager.Flush();
                LogManager.Shutdown();
                
                Console.WriteLine("TCP connection forcibly closed!");
                Console.WriteLine($"Sent {messageCount} total messages");
                Console.WriteLine();
                Console.WriteLine("*** LogReceiver should now be FROZEN waiting for new connection ***");
                Console.WriteLine("Check LogReceiver console - it should show 'Waiting for connection #2'");
                Console.WriteLine();

                // Wait a moment for the connection to fully close
                Thread.Sleep(2000);

                Console.WriteLine("Phase 4: ATTEMPTING TO SEND MESSAGES TO FROZEN LogReceiver");
                Console.WriteLine("These messages should NOT appear in LogReceiver's UI!");
                Console.WriteLine("Watch LogReceiver - the message count should stop increasing...");
                Console.WriteLine();

                // Wait a bit more for connection to fully close
                Thread.Sleep(3000);

                Console.WriteLine("Reinitializing NLog to create new connection...");
                // Reinitialize NLog to create a NEW connection attempt
                LogManager.ReconfigExistingLoggers();
                
                // Wait a moment for NLog to initialize
                Thread.Sleep(2000);
                
                // Now send test messages that should NOT appear in LogReceiver
                for (int i = 1; i <= 20 && keepRunning; i++)
                {
                    try 
                    {
                        Console.WriteLine($"  Attempting to send test message #{i}...");
                        logger.Error($"*** BUG TEST MESSAGE #{i} *** - This should NOT appear in LogReceiver UI!");
                        messageCount++;
                        
                        // Force NLog to flush each message
                        LogManager.Flush(TimeSpan.FromSeconds(2));
                        
                        Console.WriteLine($"  Sent test message #{i} - Check if it appears in LogReceiver...");
                        Thread.Sleep(1000); // 1 second between messages for easy observation
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Failed to send message #{i}: {ex.Message}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("=== BUG REPRODUCTION COMPLETE ===");
                Console.WriteLine($"Total messages sent: {messageCount}");
                Console.WriteLine("If the bug is reproduced:");
                Console.WriteLine("  - LogReceiver console shows 'Waiting for connection #2'");  
                Console.WriteLine("  - LogReceiver UI stopped updating after message ~15");
                Console.WriteLine("  - The last 20 ERROR messages did NOT appear in the UI");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit (this will NOT unblock LogReceiver)...");
                
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
