using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class SuggestTests
    {
        private static string Verbs = string.Join("|", PipelineParser.SupportedVerbs.OrderBy((s) => s));
        private static string Columns;

        [TestMethod]
        public void Suggest_Basics()
        {
            SampleDatabase.EnsureBuilt();

            Columns = string.Join("|", PipelineParser.BuildPipeline(@"
            read WebRequest
            schema", null, SampleDatabase.WorkflowContext).ToList<string>("Name").OrderBy((s) => s));

            Assert.AreEqual(Verbs, GetSuggestions(""));
            Assert.AreEqual(null, GetSuggestions($"read WebRequest"));
            Assert.AreEqual(null, GetSuggestions($"read WebRequest\r\n"));
            Assert.AreEqual(null, GetSuggestions($"read WebRequest\r\n "));
            Assert.AreEqual(Verbs, GetSuggestions($"read WebRequest\r\n_"));

            Assert.AreEqual("!=|<|<=|<>|=|==|>|>=", GetSuggestions($@"
                read WebRequest
                where HttpStatus !"));

            Assert.AreEqual("", GetSuggestions($@"
                read WebRequest
                where HttpStatus != "));

            Assert.AreEqual(Columns, GetSuggestions($@"
                read WebRequest
                columns "));
        }

        private static string GetSuggestions(string query)
        {
            try
            {
                // Try to parse the query
                PipelineParser.BuildPipeline(query, null, SampleDatabase.WorkflowContext);

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
