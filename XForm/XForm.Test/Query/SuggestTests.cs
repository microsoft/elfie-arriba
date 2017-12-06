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
        private static string Columns = string.Join("|", PipelineParser.BuildPipeline(@"
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

            Assert.AreEqual(Columns, Values(suggester.Suggest($@"
                read WebRequest
                columns ")));
        }

        private static string Values(SuggestResult result)
        {
            if (result.Usage == null || result.Usage.ValidValues == null) return null;
            return string.Join("|", result.Usage.ValidValues.OrderBy((s) => s));
        }
    }
}
