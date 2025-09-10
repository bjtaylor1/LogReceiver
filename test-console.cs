using System;
using LogReceiver;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing JsonMessageParser...");
            
            var parser = new JsonMessageParser();
            
            // Test 1: Hierarchical logger messages
            Console.WriteLine("\n=== Test 1: Hierarchical logger messages ===");
            var testMessages = new[]
            {
                @"{""time"":""2025-09-10T15:52:00.0000000Z"",""level"":""DEBUG"",""logger"":""BT"",""message"":""BT root message"",""exception"":""""}",
                @"{""time"":""2025-09-10T15:52:01.0000000Z"",""level"":""DEBUG"",""logger"":""BT.Debug"",""message"":""Debug subsystem message"",""exception"":""""}",
                @"{""time"":""2025-09-10T15:52:02.0000000Z"",""level"":""INFO"",""logger"":""BT.Debug.CommandInfo"",""message"":""Command info message"",""exception"":""""}",
                @"{""time"":""2025-09-10T15:52:03.0000000Z"",""level"":""DEBUG"",""logger"":""BT.Debug.CommandInfo.AddContentSecurityPolicyHeadersCommand"",""message"":""AddContentSecurityPolicyHeadersCommand executed"",""exception"":""""}",
                @"{""time"":""2025-09-10T15:52:04.0000000Z"",""level"":""INFO"",""logger"":""BT.Debug.CommandInfo.GetBrandedContentCommand"",""message"":""GetBrandedContentCommand executed"",""exception"":""""}",
                @"{""time"":""2025-09-10T15:52:05.0000000Z"",""level"":""ERROR"",""logger"":""BT.Error"",""message"":""Error in BT system"",""exception"":""System.Exception: Test exception""}"
            };
            
            foreach (var testMessage in testMessages)
            {
                var result = parser.ProcessString(testMessage);
                Console.WriteLine($"Input: {testMessage}");
                Console.WriteLine($"Messages found: {result.Count}");
                foreach (var msg in result)
                {
                    Console.WriteLine($"  Logger: {msg.Logger}, Message: {msg.Message}");
                }
                parser.ClearBuffer();
            }
            
            // Test 2: All messages concatenated
            Console.WriteLine("\n=== Test 2: All messages concatenated ===");
            var input2 = string.Join("", testMessages);
            var result2 = parser.ProcessString(input2);
            Console.WriteLine($"Input: {input2}");
            Console.WriteLine($"Messages found: {result2.Count}");
            foreach (var msg in result2)
            {
                Console.WriteLine($"  Message: {msg}");
            }
            
            parser.ClearBuffer();
            
            // Test 3: Messages with whitespace
            Console.WriteLine("\n=== Test 3: Messages with whitespace ===");
            var input3 = @"  {""level"": ""info"", ""message"": ""message 1""}  
                         {""level"": ""warn"", ""message"": ""message 2""}  ";
            var result3 = parser.ProcessString(input3);
            Console.WriteLine($"Input: {input3}");
            Console.WriteLine($"Messages found: {result3.Count}");
            foreach (var msg in result3)
            {
                Console.WriteLine($"  Message: {msg}");
            }
            
            parser.ClearBuffer();
            
            // Test 4: Incomplete message
            Console.WriteLine("\n=== Test 4: Incomplete message ===");
            var input4 = @"{""level"": ""info"", ""message"": ""incomple";
            var result4 = parser.ProcessString(input4);
            Console.WriteLine($"Input: {input4}");
            Console.WriteLine($"Messages found: {result4.Count}");  
            Console.WriteLine($"Buffer content: '{parser.GetBufferContent()}'");
            
            // Complete the message
            var input4b = @"te message""}";
            var result4b = parser.ProcessString(input4b);
            Console.WriteLine($"Completing with: {input4b}");
            Console.WriteLine($"Messages found: {result4b.Count}");
            foreach (var msg in result4b)
            {
                Console.WriteLine($"  Message: {msg}");
            }
            
            Console.WriteLine("\n=== All tests completed ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
