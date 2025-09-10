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
            
            // Test 1: Single message
            Console.WriteLine("\n=== Test 1: Single JSON message ===");
            var input1 = @"{""level"": ""info"", ""message"": ""message 1""}";
            var result1 = parser.ProcessString(input1);
            Console.WriteLine($"Input: {input1}");
            Console.WriteLine($"Messages found: {result1.Count}");
            foreach (var msg in result1)
            {
                Console.WriteLine($"  Message: {msg}");
            }
            
            parser.ClearBuffer();
            
            // Test 2: Two messages
            Console.WriteLine("\n=== Test 2: Two JSON messages ===");
            var input2 = @"{""level"": ""info"", ""message"": ""message 1""}{""level"": ""info"", ""message"": ""message 2""}";
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
