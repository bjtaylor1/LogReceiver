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
    }

}
