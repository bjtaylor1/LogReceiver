using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogReceiver;
using Newtonsoft.Json;

namespace LogReceiverTests
{
    [TestClass]
    public class JsonMessageParserTests
    {
        [TestMethod]
        public async Task Process_SingleMessage_CallsCallbackOnce()
        {
            // Arrange
            var input = @"{""level"": ""info"", ""message"": ""message 1""}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<MessageData>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("info", receivedMessages[0].Level);
            Assert.AreEqual("message 1", receivedMessages[0].Message);
        }

        [TestMethod]
        public async Task Process_TwoMessages_CallsCallbackTwice()
        {
            // Arrange
            var input = @"{""level"": ""info"", ""message"": ""message 1""}{""level"": ""warn"", ""message"": ""message 2""}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<MessageData>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(2, receivedMessages.Count);
            
            Assert.AreEqual("info", receivedMessages[0].Level);
            Assert.AreEqual("message 1", receivedMessages[0].Message);
            
            Assert.AreEqual("warn", receivedMessages[1].Level);
            Assert.AreEqual("message 2", receivedMessages[1].Message);
        }

        [TestMethod]
        public async Task Process_ThreeMessagesWithWhitespace_CallsCallbackThreeTimes()
        {
            // Arrange
            var input = @"  {""level"": ""info"", ""message"": ""message 1""}  
                         {""level"": ""warn"", ""message"": ""message 2""}
                         {""level"": ""error"", ""message"": ""message 3""}  ";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<MessageData>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(3, receivedMessages.Count);
            
            Assert.AreEqual("info", receivedMessages[0].Level);
            Assert.AreEqual("message 1", receivedMessages[0].Message);
            
            Assert.AreEqual("warn", receivedMessages[1].Level);
            Assert.AreEqual("message 2", receivedMessages[1].Message);
            
            Assert.AreEqual("error", receivedMessages[2].Level);
            Assert.AreEqual("message 3", receivedMessages[2].Message);
        }

        [TestMethod]
        public async Task Process_EmptyStream_CallsCallbackZeroTimes()
        {
            // Arrange
            var stream = new MemoryStream();
            var receivedMessages = new List<MessageData>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(0, receivedMessages.Count);
        }

        [TestMethod]
        public async Task Process_InvalidJson_DoesNotCallCallback()
        {
            // Arrange
            var input = @"{""level"": ""info"", ""message"": ""incomplete";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<MessageData>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(0, receivedMessages.Count);
        }

        [TestMethod]
        public async Task Process_CancellationRequested_StopsProcessing()
        {
            // Arrange
            var input = @"{""level"": ""info"", ""message"": ""message 1""}{""level"": ""warn"", ""message"": ""message 2""}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<MessageData>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            cancellationTokenSource.Cancel(); // Cancel immediately
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationTokenSource.Token);

            // Assert
            Assert.AreEqual(0, receivedMessages.Count);
        }

        [TestMethod]
        public async Task Process_WithGenericType_WorksCorrectly()
        {
            // Arrange
            var input = @"{""TestProperty"": ""test value""}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<TestMessage>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<TestMessage>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(1, receivedMessages.Count);
            Assert.AreEqual("test value", receivedMessages[0].TestProperty);
        }

        [TestMethod]
        public async Task Process_MultipleObjectsInStream_ProcessesAll()
        {
            // Arrange
            var input = @"{""level"": ""debug"", ""message"": ""debug msg""}
                         {""level"": ""info"", ""message"": ""info msg""}
                         {""level"": ""warn"", ""message"": ""warn msg""}
                         {""level"": ""error"", ""message"": ""error msg""}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var receivedMessages = new List<MessageData>();
            var cancellationToken = new CancellationToken();

            // Act
            await JsonMessageParser.ProcessAsync<MessageData>(stream, message => receivedMessages.Add(message), cancellationToken);

            // Assert
            Assert.AreEqual(4, receivedMessages.Count);
            Assert.AreEqual("debug", receivedMessages[0].Level);
            Assert.AreEqual("info", receivedMessages[1].Level);
            Assert.AreEqual("warn", receivedMessages[2].Level);
            Assert.AreEqual("error", receivedMessages[3].Level);
        }

        // Helper class for testing generic type support
        private class TestMessage
        {
            public string TestProperty { get; set; }
        }
    }
}
