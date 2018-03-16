using Cinco.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cinco.Test.Extensions
{
    [TestClass]
    public class String
    {
        [TestMethod]
        public void String_IndexOfN()
        {
            Assert.AreEqual(-1, ((string)null).IndexOfN("."));
            Assert.AreEqual(-1, ".".IndexOfN(null));
            Assert.AreEqual(-1, "".IndexOfN("."));
            Assert.AreEqual(-1, ".".IndexOfN(""));

            Assert.AreEqual(0, ".".IndexOfN("."));
            Assert.AreEqual(-1, ".".IndexOfN(".", 1));
            Assert.AreEqual(-1, ".".IndexOfN(".", 1));
        }
    }
}
