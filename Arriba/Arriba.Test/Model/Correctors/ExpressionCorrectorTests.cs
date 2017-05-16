// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model.Correctors
{
    [TestClass]
    public class ExpressionCorrectorTests
    {
        [TestMethod]
        public void Corrector_Basic()
        {
            Table t = new Table();
            TableTests.AddSampleData(t);

            // Create as many correctors as possible in the order most like real queries:
            //  - Nested ComposedCorrectors
            //  - Me Corrector on the outside
            //  - ColumnAlias corrector last
            // Unable to include UserAliasCorrector as it requires the User table
            ComposedCorrector correctors = new ComposedCorrector(new MeCorrector("scott"), new ComposedCorrector(new TodayCorrector()));

            // Verify no correction when existing correctors don't have anything to fix
            Assert.AreEqual("[t]:one", Correct("t:one", correctors));

            // Add column alias corrector and verify it's included
            correctors.Add(new ColumnAliasCorrector(t));
            Assert.AreEqual("[Title]:one", Correct("t:one", correctors));

            // Correct 'me'
            Assert.AreEqual("[a] = me OR [a] = scott", Correct("a=me", correctors));

            // Ensure 'me' is willing to correct for all column queries
            Assert.AreEqual("[*]:me OR [*]:scott", Correct("me", correctors));

            // Ensure 'me' correction does not happen to not equal and range operators (which would cause wrong results)
            Assert.AreEqual("[a] <> me", Correct("a <> me", correctors));
            Assert.AreEqual("[a] > me", Correct("a>me", correctors));

            // Correct 'today' and 'today - n'
            Assert.AreEqual(String.Format("[Created Date] > \"{0}\"", DateTime.Today.ToUniversalTime()), Correct("cd > today", correctors));
            Assert.AreEqual(String.Format("[Created Date] < \"{0}\"", DateTime.Today.AddDays(-2).ToUniversalTime()), Correct("cd < today-2", correctors));

            // Ensure 'today' is not corrected for any column
            Assert.AreEqual("[*]:today", Correct("today", correctors));

            // Verify several corrections together
            Assert.AreEqual(String.Format("[Title]:one AND [Created Date] < \"{0}\"", DateTime.Today.AddDays(-5).ToUniversalTime()), Correct("t:one cd<today-5", correctors));

            // Correct within NotExpression
            Assert.AreEqual("[Title]:one AND [Title]:two AND NOT([Title]:three)", Correct("t:one t:two -t:three", correctors));

            // Verify ComposedCorrector ensures callers don't call CorrectTerm on it.
            Verify.Exception<InvalidOperationException>(() => correctors.CorrectTerm(new TermExpression("*", Operator.Matches, "today")));
        }

        [TestMethod]
        public void UserAliasCorrector_Basic()
        {
            // Build a sample People table [for user alias correction]
            Table people = new Table();

            DataBlock block = new DataBlock(new string[] { "Alias", "Display Name" }, 3,
                new Array[] {
                    new string[] { "scott", "price", "danny" },
                    new string[] { "Scott Louvau", "Phil Price", "Danny Chen" }
                }
            );

            people.AddOrUpdate(block, new AddOrUpdateOptions() { AddMissingColumns = true });

            UserAliasCorrector corrector = new UserAliasCorrector(people);

            // People are corrected
            Assert.AreEqual("[a]:scott OR [a]:\"Scott Louvau\"", Correct("a:scott", corrector));

            // People are not corrected for all columns
            Assert.AreEqual("[*]:scott", Correct("scott", corrector));

            // No correction for unknown alias (and no errors)
            Assert.AreEqual("[a]:bob", Correct("a:bob", corrector));

            // No correction and no exception if no table was available
            Assert.AreEqual("[a]:scott", Correct("a:scott", new UserAliasCorrector(null)));
        }

        private static string Correct(string query, ICorrector correctors)
        {
            IExpression expression = QueryParser.Parse(query);
            IExpression corrected = correctors.Correct(expression);
            return corrected.ToString();
        }
    }
}
