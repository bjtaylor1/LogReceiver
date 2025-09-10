using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LogReceiver
{
    public class JsonMessageParser
    {
        private readonly MemoryStream _buffer = new MemoryStream();

        /// <summary>
        /// Processes incoming bytes and extracts complete MessageData objects.
        /// </summary>
        /// <param name="data">The incoming data bytes</param>
        /// <returns>A list of complete MessageData objects</returns>
        public List<MessageData> ProcessBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return new List<MessageData>();

            // Add new data to buffer
            _buffer.Write(data, 0, data.Length);
            
            return ExtractCompleteJsonMessages();
        }

        /// <summary>
        /// Clears the internal buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _buffer.SetLength(0);
            _buffer.Position = 0;
        }

        private List<MessageData> ExtractCompleteJsonMessages()
        {
            var messages = new List<MessageData>();
            
            if (_buffer.Length == 0)
                return messages;

            _buffer.Position = 0;
            using (var streamReader = new StreamReader(_buffer, System.Text.Encoding.UTF8, false, 1024, true))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                jsonReader.SupportMultipleContent = true;
                var serializer = new JsonSerializer();
                
                try
                {
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            var messageData = serializer.Deserialize<MessageData>(jsonReader);
                            if (messageData != null)
                            {
                                messages.Add(messageData);
                            }
                        }
                    }
                    
                    // All content was successfully parsed
                    ClearBuffer();
                }
                catch (JsonReaderException)
                {
                    // Hit incomplete JSON - keep remaining data for next time
                    if (messages.Count > 0)
                    {
                        // We parsed some complete objects, need to keep the incomplete remainder
                        var currentPosition = streamReader.BaseStream.Position;
                        var remainingData = new byte[_buffer.Length - currentPosition];
                        _buffer.Position = currentPosition;
                        _buffer.Read(remainingData, 0, remainingData.Length);
                        
                        _buffer.SetLength(0);
                        _buffer.Position = 0;
                        _buffer.Write(remainingData, 0, remainingData.Length);
                    }
                    // If no messages were parsed, keep everything in buffer
                }
            }

            return messages;
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }
    }
}
