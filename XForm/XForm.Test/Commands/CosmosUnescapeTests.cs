using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using XForm.Commands;
using XForm.Data;
using XForm.Query;

namespace XForm.Test.Commands
{
    [TestClass]
    public class CosmosUnescapeTests
    {
        [TestMethod]
        public void UnescapeCommand()
        {
            Dictionary<String8, String8> testCases = new Dictionary<String8, String8>();
            testCases.Add(String8.Empty, String8.Empty);
            testCases.Add(String8.Convert("#NULL#", new byte[6]), String8.Empty);
            testCases.Add(String8.Convert("pre#NULL#", new byte[9]), String8.Convert("pre", new byte[3]));
            testCases.Add(String8.Convert("#NULL#post", new byte[10]), String8.Convert("post", new byte[4]));
            testCases.Add(String8.Convert("pre#NULL#post", new byte[13]), String8.Convert("prepost", new byte[7]));
            testCases.Add(String8.Convert("pre#NULL#post#NULL#end", new byte[22]), String8.Convert("prepostend", new byte[10]));
            testCases.Add(String8.Convert("pre#post", new byte[8]), String8.Convert("pre#post", new byte[8]));
            testCases.Add(String8.Convert("pre#mid#post", new byte[12]), String8.Convert("pre#mid#post", new byte[12]));
            testCases.Add(String8.Convert("pre#mid#NULL#post", new byte[17]), String8.Convert("pre#midpost", new byte[11]));
            testCases.Add(String8.Convert("pre#NULL#mid#post", new byte[17]), String8.Convert("premid#post", new byte[11]));
            testCases.Add(String8.Convert("pre#NULL###NULL#post", new byte[20]), String8.Convert("pre#post", new byte[8]));
            testCases.Add(String8.Convert("pre#NULL#NULL#NULL#NULL#post", new byte[28]), String8.Convert("preNULLNULL#post", new byte[16]));
            testCases.Add(String8.Convert("pre#R##N##TAB##NULL##HASH#post", new byte[30]), String8.Convert("pre\r\n\t#post", new byte[11]));

            SampleDatabase.EnsureBuilt();
            string xqlQuery = "read WebRequest";

            // Build a Pipeline for the query. Wrap in a using statement to Dispose it when done.
            using (IDataBatchEnumerator pipeline = XqlParser.Parse(xqlQuery, null, SampleDatabase.WorkflowContext))
            {
                CosmosUnescape command = new CosmosUnescape(pipeline);

                foreach (var testCase in testCases)
                {
                    Debug.WriteLine("Testing " + testCase.Key);
                    Assert.AreEqual(testCase.Value, command.Unescape(testCase.Key, new String8Block()));
                }
            }
        }
    }
}
