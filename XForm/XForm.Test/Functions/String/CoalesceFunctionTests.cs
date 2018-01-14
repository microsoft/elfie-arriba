using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Data;
using XForm.Extensions;
using XForm.Functions.String;
using XForm.Test.Query;

namespace XForm.Test.Functions.String
{
    [TestClass]
    public class CoalesceFunctionTests
    {
        [TestMethod]
        public void Function_Coalesce()
        {
            // Coalesce returns the first non-NULL or empty string from the input columns.
            // If all values for all the columns are NULL, Coalesce returns null.
            
            // Test Cases
            // [C1] [C2] [C3] = result

            //   1    a    !  =   1
            //   2    b       =   2
            //   3            =   3 
            //        d    $  =   d
            //        e       =   e
            //             ^  =   ^
            //                =       <-- the all nulls case is not included in the test set because it is covered by the test framework


            String8Block block = new String8Block();
            String8[] column1 = new String8[]
            {
                block.GetCopy("1"),
                block.GetCopy("2"),
                block.GetCopy("3"),
                String8.Empty,
                String8.Empty,
                String8.Empty,
            };

            String8[] column2 = new String8[]
            {
                block.GetCopy("a"),
                block.GetCopy("b"),
                String8.Empty,
                block.GetCopy("d"),
                block.GetCopy("e"),
                String8.Empty,
            };

            String8[] column3 = new String8[]
            {
                block.GetCopy("!"),
                String8.Empty,
                String8.Empty,
                block.GetCopy("$"),
                String8.Empty,
                block.GetCopy("^"),
            };

            String8[] expected = new String8[]
            {
                block.GetCopy("1"),
                block.GetCopy("2"),
                block.GetCopy("3"),
                block.GetCopy("d"),
                block.GetCopy("e"),
                block.GetCopy("^"),
            };

            bool[] expectedNulls = new bool[]
            {
                false,
                false,
                false,
                false,
                false,
                false,
            };

            String8[][] inputColumnsValues = new String8[][] { column1, column2, column3 };
            bool[][] inputColumnsNulls = new bool[][] { null, null, null };
            string[] inputColumnNames = new string[] { "C1", "C2", "C3" };
            string outputColumnName = "R1";

            FunctionsTests.RunQueryAndVerify(inputColumnsValues, inputColumnsNulls, inputColumnNames, expected, expectedNulls, outputColumnName, "set [R1] Coalesce([C1], [C2], [C3])");
        }
    }
}
