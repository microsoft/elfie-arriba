// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Functions;
using XForm.Query;
using XForm.Types;

namespace XForm.Test.Query
{
    [TestClass]
    public class SuggestTests
    {
        private static string s_verbs = string.Join("|", XqlParser.SupportedVerbs.OrderBy((s) => s));
        private static string s_sources = string.Join("|", SampleDatabase.WorkflowContext.Runner.SourceNames.OrderBy((s) => s));
        private static string s_types = string.Join("|", TypeProviderFactory.SupportedTypes.OrderBy((s) => s));
        private static string s_columnNames = string.Join("|", XqlParser.Parse(@"read WebRequest", null, SampleDatabase.WorkflowContext).Columns.Select((cd) => cd.Name).OrderBy((s) => s));
        private static string s_selectListOptions = string.Join("|",
            XqlParser.Parse(@"read WebRequest", null, SampleDatabase.WorkflowContext).Columns.Select((cd) => cd.Name)
            .Concat(FunctionFactory.SupportedFunctions)
            .OrderBy((s) => s));

        [TestMethod]
        public void Suggest_Basics()
        {
            SampleDatabase.EnsureBuilt();
            QuerySuggester suggester = new QuerySuggester(SampleDatabase.WorkflowContext);

            // Verbs
            Assert.AreEqual(s_verbs, Values(suggester.Suggest("")));
            Assert.AreEqual(s_verbs, Values(suggester.Suggest("re")));

            // Tables
            Assert.AreEqual(s_sources, Values(suggester.Suggest("read")));

            // Valid
            Assert.AreEqual(null, Values(suggester.Suggest($"read WebRequest")));

            // Verbs (newline)
            Assert.AreEqual(s_verbs, Values(suggester.Suggest($"read WebRequest\r\n")));
            Assert.AreEqual(s_verbs, Values(suggester.Suggest($"read WebRequest\r\n ")));

            // CompareOperator
            Assert.AreEqual("!=|<|<=|<>|=|==|>|>=", Values(suggester.Suggest($@"
                read WebRequest
                where [HttpStatus] !")));

            // Value missing
            Assert.AreEqual(s_selectListOptions, Values(suggester.Suggest($@"
                read WebRequest
                where [HttpStatus] != ")));

            // ColumnFunctionOrLiteral
            Assert.AreEqual(s_selectListOptions, Values(suggester.Suggest($@"
                read WebRequest
                select ")));

            // Function argument (type)
            Assert.AreEqual(s_types, Values(suggester.Suggest($@"
                read WebRequest
                select Trim(Cast(Cast(5, Int32), ")));

            // Function argument (ColumnFunctionOrLiteral)
            Assert.AreEqual(s_selectListOptions, Values(suggester.Suggest($@"
                read WebRequest
                select Trim(")));

            // Nested Function argument (ColumnFunctionOrLiteral)
            Assert.AreEqual(s_selectListOptions, Values(suggester.Suggest($@"
                read WebRequest
                select Cast(Trim(")));

            // Correct nested function use
            Assert.AreEqual(true, suggester.Suggest($@"
                read WebRequest
                select Trim(Cast(Cast(5, Int32), String8)) AS [Fiver]").IsValid);
        }

        [TestMethod]
        public void Suggest_FullErrorFidelity()
        {
            SampleDatabase.EnsureBuilt();
            QuerySuggester suggester = new QuerySuggester(SampleDatabase.WorkflowContext);

            SuggestResult result = suggester.Suggest(@"
                read UsageError.WebRequest.MissingColumn");

            Assert.AreEqual(false, result.IsValid);
            Assert.AreEqual("UsageError.WebRequest.MissingColumn", result.Context.TableName);
            Assert.AreEqual(2, result.Context.QueryLineNumber);
            Assert.AreEqual("'where' [columnFunctionOrLiteral] [operator] [columnFunctionOrLiteral]", result.Context.Usage);
            Assert.AreEqual("BadColumnName", result.Context.InvalidValue);
            Assert.AreEqual("columnName", result.Context.InvalidValueCategory);
            Assert.AreEqual(s_columnNames, string.Join("|", result.Context.ValidValues));
        }

        private static string Values(SuggestResult result)
        {
            if (result.Context == null || result.Context.ValidValues == null) return null;
            return string.Join("|", result.Context.ValidValues.OrderBy((s) => s));
        }
    }
}
