using System;
using System.IO;
using System.Xml.Serialization;
using LogReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogReceiverTests
{
    [TestClass]
    public class DeserializationTests
    {
        [TestMethod]
        public void Deserialize()
        {
            var exampleMessage = "2019-10-08 16:59:23.0383|INFO|Payroll.MvcApplication|======= Application|Starting =======";

            var @event = MessageData.Parse(exampleMessage);
            Assert.AreEqual("======= Application|Starting =======", @event.Message);
        }

        [TestMethod]
        public void DeserializeWithSequenceId()
        {
            var exampleMessage = "123|2019-10-08 16:59:23.0383|INFO|Payroll.MvcApplication|======= Application|Starting =======|123";

            var @event = MessageData.Parse(exampleMessage);
            Assert.AreEqual("======= Application|Starting =======", @event.Message);
            Assert.AreEqual("INFO", @event.Level);
            Assert.AreEqual("Payroll.MvcApplication", @event.Logger);
        }

        [TestMethod]
        public void DeserializeWithSequenceIdAndComplexMessage()
        {
            var exampleMessage = "456|2019-10-08 16:59:23.0383|ERROR|BT.Debug.Logger|Error occurred: User|Name contains|pipe characters|456";

            var @event = MessageData.Parse(exampleMessage);
            Assert.AreEqual("Error occurred: User|Name contains|pipe characters", @event.Message);
            Assert.AreEqual("ERROR", @event.Level);
            Assert.AreEqual("BT.Debug.Logger", @event.Logger);
        }
    }

}
