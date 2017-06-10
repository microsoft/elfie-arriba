using Microsoft.VisualStudio.TestTools.UnitTesting;
using V5.Collections;

namespace V5.Test.Collections
{
    [TestClass]
    public class IndexSetTests
    {
        [TestMethod]
        public void IndexSet_Basics()
        {
            IndexSet set = new IndexSet(0, 999);

            Assert.AreEqual(0, set.Count, "Set should start empty");

            set.All();
            Assert.AreEqual(999, set.Count, "All should set through length only.");

            set.None();
            Assert.AreEqual(0, set.Count, "None should clear");

            set[0] = true;
            Assert.IsTrue(set[0]);
            Assert.IsFalse(set[63]);

            set[63] = true;
            Assert.IsTrue(set[0]);
            Assert.IsTrue(set[63]);

            set[0] = false;
            Assert.IsFalse(set[0]);
            Assert.IsTrue(set[63]);

            byte[] values = new byte[999];
            for(int i = 0; i < values.Length; ++i)
            {
                values[i] = (byte)(i & 255);
            }

            set.All().And(values, Query.Operator.GreaterThan, (byte)254);
            Assert.AreEqual(3, set.Count);

        }
    }
}
