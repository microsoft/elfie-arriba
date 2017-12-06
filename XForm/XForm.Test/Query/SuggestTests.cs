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
        private static string Sources = string.Join("|", SampleDatabase.WorkflowContext.Runner.SourceNames.OrderBy((s) => s));
        private static string WebRequestColumns = string.Join("|", PipelineParser.BuildPipeline(@"
            read WebRequest
            schema", null, SampleDatabase.WorkflowContext).ToList<string>("Name").OrderBy((s) => s));

        [TestMethod]
        public void Suggest_Basics()
        {
            SampleDatabase.EnsureBuilt();
            QuerySuggester suggester = new QuerySuggester(SampleDatabase.WorkflowContext);

            Assert.AreEqual(Verbs, Values(suggester.Suggest("")));
            Assert.AreEqual(Verbs, Values(suggester.Suggest("re")));
            Assert.AreEqual(Sources, Values(suggester.Suggest("read")));
            Assert.AreEqual("", Values(suggester.Suggest($"read WebRequest")));
            Assert.AreEqual(Verbs, Values(suggester.Suggest($"read WebRequest\r\n")));
            Assert.AreEqual(Verbs, Values(suggester.Suggest($"read WebRequest\r\n ")));

            Assert.AreEqual("!=|<|<=|<>|=|==|>|>=", Values(suggester.Suggest($@"
                read WebRequest
                where HttpStatus !")));

            Assert.AreEqual("", Values(suggester.Suggest($@"
                read WebRequest
                where HttpStatus != ")));

            Assert.AreEqual(WebRequestColumns, Values(suggester.Suggest($@"
                read WebRequest
                columns ")));
        }

        [TestMethod]
        public void Suggest_FullErrorFidelity()
        {
            SampleDatabase.EnsureBuilt();
            QuerySuggester suggester = new QuerySuggester(SampleDatabase.WorkflowContext);

            SuggestResult result = suggester.Suggest(@"
                read UsageError.WebRequest.MissingColumn");

            Assert.AreEqual(false, result.IsValid);
            Assert.AreEqual("UsageError.WebRequest.MissingColumn", result.Usage.TableName);
            Assert.AreEqual("where BadColumnName != \"\"", result.Usage.QueryLine);
            Assert.AreEqual("'where' [columnName] [operator] [value]", result.Usage.Usage);
            Assert.AreEqual("BadColumnName", result.Usage.InvalidValue);
            Assert.AreEqual("columnName", result.Usage.InvalidValueCategory);
            Assert.AreEqual(WebRequestColumns, string.Join("|", result.Usage.ValidValues));
            
        }

        private static string Values(SuggestResult result)
        {
            if (result.Usage == null || result.Usage.ValidValues == null) return null;
            return string.Join("|", result.Usage.ValidValues.OrderBy((s) => s));
        }
    }
}
