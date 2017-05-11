using Arriba.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class IpRangeColumnTests
    {
        [TestMethod]
        public void IpRange_Parsing()
        {
            // Full single IP
            Assert.AreEqual("10.1.15.230", ParseAndToString("10.1.15.230"));

            // Partial IPs -> Ranges
            Assert.AreEqual("10.1.15.0-10.1.15.255", ParseAndToString("10.1.15"));
            Assert.AreEqual("10.0.0.0-10.255.255.255", ParseAndToString("10"));
            Assert.AreEqual("10.0.0.0-10.255.255.255", ParseAndToString("10.*"));
            Assert.AreEqual("10.1.0.0-10.1.255.255", ParseAndToString("10.1.*"));
            Assert.AreEqual("0.0.0.0-255.255.255.255", ParseAndToString("*"));

            // IP Ranges
            Assert.AreEqual("10.1.15.10-10.1.15.200", ParseAndToString("10.1.15.10-10.1.15.200"));
            Assert.AreEqual("192.168.254.6-192.168.254.64", ParseAndToString("192.168.254.6-192.168.254.64"));
            Assert.AreEqual("10.1.15.230", ParseAndToString("10.1.15.230-10.1.15.230"));

            // CIDR Ranges
            Assert.AreEqual("10.11.0.0-10.11.255.255", ParseAndToString("10.11.0.0/16"));
            Assert.AreEqual("192.168.100.0-192.168.103.255", ParseAndToString("192.168.100.0/22"));
            Assert.AreEqual("192.168.100.0-192.168.100.255", ParseAndToString("192.168.100.15/24"));

            // Null, Empty, text
            Assert.AreEqual("", ParseAndToString(null));
            Assert.AreEqual("", ParseAndToString(""));
            Assert.AreEqual("", ParseAndToString("Sophie"));

            // Number but out of range
            Assert.AreEqual("", ParseAndToString("333"));
            Assert.AreEqual("", ParseAndToString("10.1.256.255"));
            Assert.AreEqual("", ParseAndToString("10.1.10.1.10"));

            // Bad ranges
            Assert.AreEqual("", ParseAndToString("192.168.254.6-192.168.254.5"));
            Assert.AreEqual("", ParseAndToString("192.168.254-192.168.256"));
        }

        private string ParseAndToString(string ipRange)
        {
            IpRange result;
            if(IpRange.TryParse(ipRange, out result))
            {
                return result.ToString();
            }

            return "";
        }
    }
}
