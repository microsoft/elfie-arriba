using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class SuggestTests
    {
        private static string Verbs = string.Join("|", PipelineParser.SupportedVerbs.OrderBy((s) => s));
        [TestMethod]
        public void Suggest_Basics()
        {
            DataBatchEnumeratorTests.WriteSamples();

            Assert.AreEqual(Verbs, GetSuggestions(""));
            Assert.AreEqual(null, GetSuggestions($"read {DataBatchEnumeratorTests.WebRequestSample}"));
            Assert.AreEqual(null, GetSuggestions($"read {DataBatchEnumeratorTests.WebRequestSample}\r\n"));
            Assert.AreEqual(null, GetSuggestions($"read {DataBatchEnumeratorTests.WebRequestSample}\r\n "));
            Assert.AreEqual(Verbs, GetSuggestions($"read {DataBatchEnumeratorTests.WebRequestSample}\r\n_"));

            Assert.AreEqual("!=|<|<=|<>|=|==|>|>=", GetSuggestions($@"
                read {DataBatchEnumeratorTests.WebRequestSample}
                where HttpStatus !"));

            Assert.AreEqual("", GetSuggestions($@"
                read {DataBatchEnumeratorTests.WebRequestSample}
                where HttpStatus != "));

            Assert.AreEqual("", GetSuggestions($@"
                read {DataBatchEnumeratorTests.WebRequestSample}
                columns "));
        }

        private static string GetSuggestions(string query)
        {
            try
            {
                // Try to parse the query
                PipelineParser.BuildPipeline(query, null, null);

                // If valid, no suggestions
                return null;
            }
            catch (UsageException ex)
            {
                if (ex.ValidValues == null) return "";
                return string.Join("|", ex.ValidValues);
            }
        }
    }
}
