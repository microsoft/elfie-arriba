using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Commands;
using XForm.Data;
using XForm.Query;

namespace XForm.Test.Data
{
    [TestClass]
    public class ReplaceTests
    {
        [TestMethod]
        public void ReplaceCommand()
        {
            List<Tuple<String8, String8, string, string>> testCases = new List<Tuple<String8, String8, string, string>>();
            testCases.Add(Tuple.Create(String8.Empty, String8.Empty, "find", "replace"));
            testCases.Add(Tuple.Create(String8.Convert("N/A", new byte[3]), String8.Empty, "N/A", String.Empty));
            testCases.Add(Tuple.Create(String8.Convert("N/A", new byte[3]), String8.Convert("Not Applicable", new byte[14]), "N/A", "Not Applicable"));
            testCases.Add(Tuple.Create(String8.Convert("the quick brown fox", new byte[19]), String8.Convert("the quick red fox", new byte[17]), "brown", "red"));

            SampleDatabase.EnsureBuilt();
            string xqlQuery = "read WebRequest";

            // Build a Pipeline for the query. Wrap in a using statement to Dispose it when done.
            using (IDataBatchEnumerator pipeline = XqlParser.Parse(xqlQuery, null, SampleDatabase.WorkflowContext))
            {
                foreach (var testCase in testCases)
                {
                    Debug.WriteLine("Testing " + testCase.ToString());
                    Replace command = new Replace(pipeline, testCase.Item3, testCase.Item4);
                    Assert.AreEqual(testCase.Item2, command.ReplaceText(testCase.Item1, new String8Block()));
                }
            }
        }
    }
}
