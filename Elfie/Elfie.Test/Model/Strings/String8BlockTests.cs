using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Strings
{
    [TestClass]
    public class String8BlockTests
    {
        [TestMethod]
        public void String8Block_Basics()
        {
            String8Block block = new String8Block();

            byte[] buffer = new byte[4096];
            String8 value = String8.Convert("hello", buffer);

            // Verify copies are persistent when the original buffer is overwritten
            String8 valueCopy = block.GetCopy(value);
            String8.Convert("there", buffer);
            Assert.AreEqual("there", value.ToString());
            Assert.AreEqual("hello", valueCopy.ToString());

            // Verify copy of String8.Empty works
            String8 emptyCopy = block.GetCopy(String8.Empty);
            Assert.IsTrue(emptyCopy.IsEmpty());

            // Verify large strings are copied correctly (stored individually)
            value = String8.Convert(new string('A', 4096), buffer);
            valueCopy = block.GetCopy(value);
            Assert.IsTrue(value.Equals(valueCopy));
            String8.Convert(new string('B', 4096), buffer);
            Assert.IsFalse(value.Equals(valueCopy));

            // Verify storage uses multiple blocks correctly
            for(int i = 0; i < 1000; ++i)
            {
                value = String8.Convert(new string((char)('0' + (i % 10)), 100), buffer);
                valueCopy = block.GetCopy(value);
                Assert.IsTrue(value.Equals(valueCopy));
            }
        }
    }
}
