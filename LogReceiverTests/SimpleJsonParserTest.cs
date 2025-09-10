using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogReceiver;

namespace LogReceiverTests
{
    [TestClass]
    public class SimpleJsonParserTest
    {
        [TestMethod]
        public void TestJsonParserCreation()
        {
            // Arrange & Act
            var parser = new JsonMessageParser();

            // Assert
            Assert.IsNotNull(parser);
            // Test that empty buffer returns no messages
            var result = parser.ProcessBytes(new byte[0]);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestSingleJsonMessage()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""message 1""}";
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(1, result.Count, "Should return exactly 1 message");
            // Compare MessageData properties directly
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("message 1", result[0].Message);
        }

        [TestMethod]  
        public void TestTwoJsonMessages()
        {
            // Arrange
            var parser = new JsonMessageParser();
            var input = @"{""level"": ""info"", ""message"": ""message 1""}{""level"": ""info"", ""message"": ""message 2""}";
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);

            // Act
            var result = parser.ProcessBytes(inputBytes);

            // Assert
            Assert.AreEqual(2, result.Count, "Should return exactly 2 messages");
            
            // Verify first message
            Assert.AreEqual("info", result[0].Level);
            Assert.AreEqual("message 1", result[0].Message);
            
            // Verify second message
            Assert.AreEqual("info", result[1].Level);
            Assert.AreEqual("message 2", result[1].Message);
        }
    }
}
