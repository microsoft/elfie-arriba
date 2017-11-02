// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            for (int i = 0; i < 1000; ++i)
            {
                value = String8.Convert(new string((char)('0' + (i % 10)), 100), buffer);
                valueCopy = block.GetCopy(value);
                Assert.IsTrue(value.Equals(valueCopy));
            }

            // Verify conversion of strings
            String8 directConversion = block.GetCopy("Regular String");
            Assert.AreEqual("Regular String", directConversion.ToString());

            // Verify null/empty string conversion
            directConversion = block.GetCopy((string)null);
            Assert.IsTrue(directConversion.IsEmpty());

            directConversion = block.GetCopy(String.Empty);
            Assert.IsTrue(directConversion.IsEmpty());

            // Verify clear works (doesn't throw, GetCopy works afterward)
            block.Clear();
            valueCopy = block.GetCopy("Third");
            Assert.AreEqual("Third", valueCopy.ToString());
        }

        [TestMethod]
        public void String8Block_Concatenate()
        {
            String8Block block = new String8Block();
            String8 delimiter = block.GetCopy("; ");
            String8 one = block.GetCopy("One");
            String8 two = block.GetCopy("Two");
            String8 three = block.GetCopy("Three");

            // Verify Concatenate returns only one side if the other is empty
            Assert.AreEqual(String8.Empty, block.Concatenate(String8.Empty, delimiter, String8.Empty));
            Assert.AreEqual(two, block.Concatenate(String8.Empty, delimiter, two));
            Assert.AreEqual(two, block.Concatenate(two, delimiter, String8.Empty));

            // Verify a regular concatenation
            String8 oneTwo = block.Concatenate(one, delimiter, two);
            Assert.AreEqual("One; Two", oneTwo.ToString());

            // Verify re-concatenating the last item re-uses memory and doesn't mess up previous item
            String8 oneTwoThree = block.Concatenate(oneTwo, delimiter, three);
            Assert.AreEqual(oneTwo.Array, oneTwoThree.Array);
            Assert.AreEqual(oneTwo.Index, oneTwoThree.Index);

            Assert.AreEqual("One; Two", oneTwo.ToString());
            Assert.AreEqual("One; Two; Three", oneTwoThree.ToString());

            // Verify re-concatenating doesn't overwrite a following item
            String8 four = block.GetCopy("Four");
            String8 oneTwoThreeFour = block.Concatenate(oneTwoThree, delimiter, four);
            Assert.AreEqual("One; Two; Three", oneTwoThree.ToString());
            Assert.AreEqual("One; Two; Three; Four", oneTwoThreeFour.ToString());

            // Concatenate over the 64K limit and ensure reasonable behavior
            String8 eight = block.GetCopy("12345678");
            String8 eightSet = String8.Empty;
            for (int i = 0; i < 10000; ++i)
            {
                eightSet = block.Concatenate(eightSet, delimiter, eight);
            }
        }
    }
}
