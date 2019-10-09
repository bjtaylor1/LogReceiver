using LogReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogReceiverTests
{
    [TestClass]
    public class MapperTests
    {
        [TestMethod]
        public void MainMappingProfile()
        {
            Mapping.GetConfiguration().AssertConfigurationIsValid();
        }
    }
}
