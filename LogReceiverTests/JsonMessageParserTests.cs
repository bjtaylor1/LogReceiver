using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogReceiver;
using Newtonsoft.Json;

namespace LogReceiverTests
{
    [TestClass]
    public class JsonMessageParserTests
    {
        [TestMethod]
        public void ProcessBytes_SingleMessage_ReturnsOneMessage()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""message 1""}";
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("message 1", result[0].Message);
        }

        [TestMethod]
        public void ProcessBytes_TwoMessages_ReturnsTwoMessages()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""message 1""}{""level"": ""info"", ""message"": ""message 2""}";
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(2, result.Count);
            
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("message 1", result[0].Message);
            
            Assert.AreEqual("info", result[1].Level);
            Assert.AreEqual("message 2", result[1].Message);
        }

        [TestMethod]
        public void ProcessBytes_ThreeMessagesWithWhitespace_ReturnsThreeMessages()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"  {""level"": ""info"", ""message"": ""message 1""}  
                         {""level"": ""warn"", ""message"": ""message 2""}
                         {""level"": ""error"", ""message"": ""message 3""}  ";
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(3, result.Count);
            
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("message 1", result[0].Message);
            
            Assert.AreEqual("warn", result[1].Level);
            Assert.AreEqual("message 2", result[1].Message);
            
            Assert.AreEqual("error", result[2].Level);
            Assert.AreEqual("message 3", result[2].Message);
        }



        [TestMethod]
        public void ProcessBytes_IncompleteMessage_ReturnsNoMessages()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""incomplete";
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(0, result.Count);
            
            // Test that buffer retains incomplete data by completing the message
            var completionBytes = Encoding.UTF8.GetBytes(@"""}");
            var result2 = parser.ProcessBytes(completionBytes);
            Assert.AreEqual(1, result2.Count);
            Assert.AreEqual("info", result2[0].Level);
            Assert.AreEqual("incomplete", result2[0].Message);
        }

        [TestMethod]
        public void ProcessBytes_PartialThenComplete_HandlesCorrectly()
        {
            // Arrange
            var parser = new JsonMessageParser();

            // Act - First, send incomplete message
            var bytes1 = Encoding.UTF8.GetBytes(@"{""level"": ""info"", ""mess");
            var result1 = parser.ProcessBytes(bytes1);
            
            // Then complete it
            var bytes2 = Encoding.UTF8.GetBytes(@"age"": ""complete message""}");
            var result2 = parser.ProcessBytes(bytes2);

            // Assert
            Assert.AreEqual(0, result1.Count);
            Assert.AreEqual(1, result2.Count);
            
            Assert.AreEqual("info", result2[0].Level);
            Assert.AreEqual("complete message", result2[0].Message);
            
            // Test that buffer is empty by processing empty data and expecting no results
            var emptyResult = parser.ProcessBytes(new byte[0]);
            Assert.AreEqual(0, emptyResult.Count);
        }

        [TestMethod]
        public void ProcessBytes_CompleteAndPartialMessage_ReturnsCompleteOnly()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""complete""}{""level"": ""warn"", ""partial"": ""inc";
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("complete", result[0].Message);
            
            // Test that partial message is buffered by completing it
            var completionBytes = Encoding.UTF8.GetBytes(@"omplete""}");
            var result2 = parser.ProcessBytes(completionBytes);
            Assert.AreEqual(1, result2.Count);
        }

        [TestMethod]
        public void ProcessBytes_WithByteArray_WorksCorrectly()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""test""}";
            var bytes = Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(bytes);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("test", result[0].Message);
        }

        [TestMethod]
        public void ProcessBytes_WithLengthParameter_WorksCorrectly()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""test""}";
            var bytes = Encoding.UTF8.GetBytes(input + "extra data");

            // Act
            var relevantBytes = new byte[Encoding.UTF8.GetByteCount(input)];
            Array.Copy(bytes, relevantBytes, relevantBytes.Length);
            var result = parser.ProcessBytes(relevantBytes);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("test", result[0].Message);
            
            // Test that buffer is empty
            var emptyResult = parser.ProcessBytes(new byte[0]);
            Assert.AreEqual(0, emptyResult.Count);
        }



        [TestMethod]
        public void ClearBuffer_RemovesBufferedContent()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var incompleteBytes = Encoding.UTF8.GetBytes(@"{""incomplete"": ""mess");
            parser.ProcessBytes(incompleteBytes);

            // Act
            parser.ClearBuffer();

            // Assert - Test that buffer is cleared by trying to complete the message
            var completionBytes = Encoding.UTF8.GetBytes(@"age""}");
            var result = parser.ProcessBytes(completionBytes);
            Assert.AreEqual(0, result.Count); // Should return no messages since buffer was cleared
        }


    }
}
