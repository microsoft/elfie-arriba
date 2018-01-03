using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Data;
using XForm.Functions.String;

namespace XForm.Test.Functions.String
{
    [TestClass]
    public class CoalesceFunctionTests
    {
        [TestMethod]
        public void Coalesce()
        {
            // Arrange

            String8[] transformedArray = null;
            bool[] nullArray = null;

            // 1 a !   =   1
            // 2 b -   =   2
            // 3 - -   =   3 
            // - d $   =   d
            // - e -   =   e
            // - - ^   =   ^
            // - - -   =   -


            String8[] column1 = new String8[]
            {
                "1".ToString8(),
                "2".ToString8(),
                "3".ToString8(),
                String8.Empty,
                String8.Empty,
                String8.Empty,
                String8.Empty,
            };

            String8[] column2 = new String8[]
            {
                "a".ToString8(),
                "b".ToString8(),
                String8.Empty,
                "d".ToString8(),
                "e".ToString8(),
                String8.Empty,
                String8.Empty,
            };

            String8[] column3 = new String8[]
            {
                "!".ToString8(),
                String8.Empty,
                String8.Empty,
                "$".ToString8(),
                String8.Empty,
                "^".ToString8(),
                String8.Empty,
            };

            String8[] expected = new String8[]
            {
                "1".ToString8(),
                "2".ToString8(),
                "3".ToString8(),
                "d".ToString8(),
                "e".ToString8(),
                "^".ToString8(),
                String8.Empty,
            };


            // Act
            DataBatch actual = CoalesceColumn.CoalesceBatch(ref transformedArray, ref nullArray, DataBatch.All(column1), DataBatch.All(column2), DataBatch.All(column3));


            // Assert
            Assert.AreEqual(expected.Length, actual.Count, "The coalesced column length did not match the expected column length.");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual.Array.GetValue(i));
            }
        }
    }

    static class String8Extensions
    {
        public static String8 ToString8(this string text)
        {
            return String8.Convert(text, new byte[String8.GetLength(text)]);
        }
    }
}
