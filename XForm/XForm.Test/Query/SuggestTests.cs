// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Query
{
    [TestClass]
    public class SuggestTests
    {
        private static string s_verbs = string.Join("|", XqlParser.SupportedVerbs.OrderBy((s) => s));
        private static string s_sources = string.Join("|", SampleDatabase.WorkflowContext.Runner.SourceNames.OrderBy((s) => s));
        private static string s_webRequestColumns = string.Join("|", XqlParser.Parse(@"
            read WebRequest
            schema", null, SampleDatabase.WorkflowContext).ToList<string>("Name").OrderBy((s) => s));

        [TestMethod]
        public void Suggest_Basics()
        {
            SampleDatabase.EnsureBuilt();
            QuerySuggester suggester = new QuerySuggester(SampleDatabase.WorkflowContext);

            Assert.AreEqual(s_verbs, Values(suggester.Suggest("")));
            Assert.AreEqual(s_verbs, Values(suggester.Suggest("re")));
            Assert.AreEqual(s_sources, Values(suggester.Suggest("read")));
            Assert.AreEqual("", Values(suggester.Suggest($"read WebRequest")));
            Assert.AreEqual(s_verbs, Values(suggester.Suggest($"read WebRequest\r\n")));
            Assert.AreEqual(s_verbs, Values(suggester.Suggest($"read WebRequest\r\n ")));

            Assert.AreEqual("!=|<|<=|<>|=|==|>|>=", Values(suggester.Suggest($@"
                read WebRequest
                where [HttpStatus] !")));

            Assert.AreEqual("", Values(suggester.Suggest($@"
                read WebRequest
                where [HttpStatus] != ")));

            Assert.AreEqual(s_webRequestColumns, Values(suggester.Suggest($@"
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
            Assert.AreEqual(2, result.Usage.QueryLineNumber);
            Assert.AreEqual("'where' [columnName] [operator] [value]", result.Usage.Usage);
            Assert.AreEqual("BadColumnName", result.Usage.InvalidValue);
            Assert.AreEqual("columnName", result.Usage.InvalidValueCategory);
            Assert.AreEqual(s_webRequestColumns, string.Join("|", result.Usage.ValidValues));
        }

        private static string Values(SuggestResult result)
        {
            if (result.Usage == null || result.Usage.ValidValues == null) return null;
            return string.Join("|", result.Usage.ValidValues.OrderBy((s) => s));
        }
    }
}
