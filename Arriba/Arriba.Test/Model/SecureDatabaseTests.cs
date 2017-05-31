// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class SecureDatabaseTests
    {
        private const string sampleTableName = "Sample";

        private SecureDatabase BuildSampleDB()
        {
            SecureDatabase db = new SecureDatabase();

            // Add a sample table
            if (db.TableExists(sampleTableName)) db.DropTable(sampleTableName);
            Table t = db.AddTable(sampleTableName, 100000);
            t.AddColumn(new ColumnDetails("ID", "int", null, null, true));
            t.AddColumn(new ColumnDetails("Title", "string", ""));
            t.AddColumn(new ColumnDetails("Priority", "byte", 0));
            t.AddColumn(new ColumnDetails("SecretOwner", "string", null));
            t.AddColumn(new ColumnDetails("SecretPriority", "byte", null));

            // Add sample data (for joins)
            DataBlock b = new DataBlock(new string[] { "ID", "Title", "Priority", "SecretOwner", "SecretPriority" }, 5,
                new Array[]
                {
                    new int[] { 1, 2, 3, 4, 5 },
                    new string[] { "One", "Two", "Three", "Four", "Five" },
                    new byte[] { 1, 1, 3, 3, 0 },
                    new string[] { "Bob", "Alice", "Alice", "Bob", "Bob" },
                    new byte[] { 3, 3, 2, 2, 0 }
                });
            t.AddOrUpdate(b);

            return db;
        }

        private void SecureSampleDB(SecureDatabase db)
        {
            SecurityPermissions security = db.Security(sampleTableName);
            security.RestrictedColumns.Add(SecurityIdentity.Create(IdentityScope.Group, "G1"), new List<string>(new string[] { "SecretOwner" }));
            security.RestrictedColumns.Add(SecurityIdentity.Create(IdentityScope.Group, "G2"), new List<string>(new string[] { "SecretPriority" }));
            security.RowRestrictedUsers.Add(SecurityIdentity.Create(IdentityScope.Group, "G3"), "SecretPriority > 1");
            security.RowRestrictedUsers.Add(SecurityIdentity.Create(IdentityScope.Group, "G4"), "SecretPriority > 2");
        }

        [TestMethod]
        public void SecureDatabase_SelectQuerySecurity()
        {
            SecureDatabase db = BuildSampleDB();

            SelectQuery q = new SelectQuery() { TableName = sampleTableName, Columns = new string[] { "*" }, Where = QueryParser.Parse("One") };
            SelectResult result;

            // Verify default 'Query' method throws (must pass method which can report user group memberships)
            Verify.Exception<ArribaException>(() => db.Query(new SelectQuery(q)));

            // Run the Query without security. Expect no restrictions and no security checks.
            result = db.Query(new SelectQuery(q), (si) => { Assert.Fail("No security checks for unsecured table"); return true; });
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, SecretOwner, SecretPriority, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Restrict the secret columns to people in "G1", or users in "G2" can see everything but only for SecretPriority < 1 items.
            SecureSampleDB(db);

            // Run the Query as a user in all column restriction groups
            // Verify the query is unrestricted - no WHERE clause and no filtered columns
            result = db.Query(new SelectQuery(q), (si) => si.Name == "g1" || si.Name == "g2");
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, SecretOwner, SecretPriority, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Run the query as a user in G1 only; verify G2 restricted columns excluded
            result = db.Query(new SelectQuery(q), (si) => si.Name == "g1");
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsTrue(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, SecretOwner, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Run the Query as a user in G3.
            // Verify WHERE clause restriction, but no column restrictions
            // Security design is EITHER row or column security.
            result = db.Query(new SelectQuery(q), (si) => si.Name == "g3");
            Assert.AreEqual("[SecretPriority] > 1 AND [*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, SecretOwner, SecretPriority, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Run the Query as a user in G4.
            // Verify WHERE clause restriction, but no column restrictions
            // Security design is EITHER row or column security.
            result = db.Query(new SelectQuery(q), (si) => si.Name == "g4");
            Assert.AreEqual("[SecretPriority] > 2 AND [*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, SecretOwner, SecretPriority, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Run the Query as a user in all groups
            // Verify WHERE clause restriction for *first* matching group, no column restrictions
            result = db.Query(new SelectQuery(q), (si) => true);
            Assert.AreEqual("[SecretPriority] > 1 AND [*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, SecretOwner, SecretPriority, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Run the Query as a user in no groups.
            // Verify all column restrictions, no where clause filter
            result = db.Query(new SelectQuery(q), (si) => false);
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsTrue(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("ID, Priority, Title", String.Join(", ", ((SelectQuery)result.Query).Columns));

            // Add a query clause for a restricted column when not in the required group. Verify error.
            result = db.Query(new SelectQuery(q) { Where = QueryParser.Parse("One AND SecretOwner=Bob") }, (si) => false);
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretOwner"), result.Details.Errors);

            // Add a query clause for a restricted column when in the required group. Verify success.
            result = db.Query(new SelectQuery(q) { Where = QueryParser.Parse("One AND SecretOwner=Bob") }, (si) => si.Name == "g1");
            Assert.AreEqual("[*]:One AND [SecretOwner] = Bob", result.Query.Where.ToString());
            Assert.IsTrue(HasRestrictedClauses(result.Query.Where));

            // Ask for restricted columns in result listing. Verify restricted columns allowed only for my group, warning for removed column.
            result = db.Query(new SelectQuery(q) { Columns = new string[] { "Title", "SecretOwner", "SecretPriority" } }, (si) => si.Name == "g1");
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsTrue(HasRestrictedClauses(result.Query.Where));
            Assert.AreEqual("Title, SecretOwner", String.Join(", ", ((SelectQuery)result.Query).Columns));
            Assert.AreEqual("SecretPriority", String.Join(", ", result.Details.AccessDeniedColumns));
        }

        /// <summary>
        ///  Recursively check to see if any clauses in the query were restricted.
        /// </summary>
        /// <param name="expression">IExpression to check</param>
        /// <returns>True if any column restricted clauses, False otherwise</returns>
        private static bool HasRestrictedClauses(IExpression expression)
        {
            if (expression is AllExceptColumnsTermExpression) return true;

            foreach (IExpression child in expression.Children())
            {
                if (HasRestrictedClauses(child)) return true;
            }

            return false;
        }

        [TestMethod]
        public void SecureDatabase_AggregationQuerySecurity()
        {
            SecureDatabase db = BuildSampleDB();

            AggregationQuery q = new AggregationQuery("sum", new string[] { "ID" }, "One");
            q.TableName = sampleTableName;

            AggregationResult result;

            // Run the Query without security. Expect no restrictions and no security checks.
            result = db.Query(new AggregationQuery(q), (si) => { Assert.Fail("No security checks for unsecured table"); return true; });
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());

            // Restrict the secret columns to people in "G1", or users in "G2" can see everything but only for SecretPriority < 1 items.
            SecureSampleDB(db);

            // Run the Query as a user in all column restriction groups
            // Verify the query is unrestricted - no WHERE clause and no filtered columns
            result = db.Query(new AggregationQuery(q), (si) => si.Name == "g1" || si.Name == "g2");
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));

            // Run the Query as a user in G3.
            // Verify WHERE clause restriction.
            result = db.Query(new AggregationQuery(q), (si) => si.Name == "g3");
            Assert.AreEqual("[SecretPriority] > 1 AND [*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));

            // Run the Query as a user in all groups
            // Verify WHERE clause restriction for *first* matching group
            result = db.Query(new AggregationQuery(q), (si) => true);
            Assert.AreEqual("[SecretPriority] > 1 AND [*]:One", result.Query.Where.ToString());
            Assert.IsFalse(HasRestrictedClauses(result.Query.Where));

            // Run the Query as a user in no groups.
            // Verify all column restrictions, no where clause filter
            result = db.Query(new AggregationQuery(q), (si) => false);
            Assert.AreEqual("[*]:One", result.Query.Where.ToString());
            Assert.IsTrue(HasRestrictedClauses(result.Query.Where));

            // Add a query clause for a disallowed column when not in group. Verify error.
            result = db.Query(new AggregationQuery(q) { Where = QueryParser.Parse("One AND SecretOwner=Bob") }, (si) => false);
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretOwner"), result.Details.Errors);

            // Add a query clause for a disallowed column when in group. Verify success.
            result = db.Query(new AggregationQuery(q) { Where = QueryParser.Parse("One AND SecretOwner=Bob") }, (si) => si.Name == "g1");
            Assert.AreEqual("[*]:One AND [SecretOwner] = Bob", result.Query.Where.ToString());
            Assert.IsTrue(HasRestrictedClauses(result.Query.Where));

            // Ask to aggregate on a restricted column. Verify error.
            result = db.Query(new AggregationQuery(q) { AggregationColumns = new string[] { "SecretPriority" } }, (si) => si.Name == "g1");
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretPriority"), result.Details.Errors);

            // Ask to aggregate over a restricted column dimension. Verify error.
            AggregationQuery a = new AggregationQuery(q);
            a.Dimensions.Add(new AggregationDimension("Columns", new string[] { "ID > 5", "ID > 10", "SecretPriority = 1" }));

            result = db.Query(a, (si) => si.Name == "g1");
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretPriority"), result.Details.Errors);
        }

        [TestMethod]
        public void SecureDatabase_JoinQuerySecurity()
        {
            SecureDatabase db = BuildSampleDB();
            SecureSampleDB(db);

            JoinQuery<SelectResult> q = new JoinQuery<SelectResult>(db,
                new SelectQuery() { TableName = sampleTableName, Columns = new string[] { "*" } },
                new SelectQuery[] {
                    new SelectQuery() { TableName = sampleTableName, Where = QueryParser.Parse("One") },
                    new SelectQuery() { TableName = sampleTableName, Where = QueryParser.Parse("SecretOwner=Bob") }
                });

            SelectResult result;

            // Run a JOIN with full access - verify success (inner query expanded).
            q.Where = QueryParser.Parse("SecretPriority = 1 AND SecretOwner = #Q2[SecretOwner]");
            result = db.Query(q, (si) => si.Name == "g1" || si.Name == "g2");
            Assert.IsTrue(result.Details.Succeeded);
            Assert.AreEqual("[SecretPriority] = 1 AND [SecretOwner] = IN(Bob, Bob, Bob)", result.Query.Where.ToString());

            // Run a JOIN with a disallowed clause on the top query - verify error
            q.Where = QueryParser.Parse("SecretPriority = 1 AND SecretOwner = #Q1[SecretOwner]");
            result = db.Query(q, (si) => si.Name == "g2");
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretOwner"), result.Details.Errors);

            // Run a JOIN where the join column in the outer query is disallowed - verify error
            q.Where = QueryParser.Parse("ID > 1 AND SecretOwner = #Q1[ID]");
            result = db.Query(q, (si) => false);
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretOwner"), result.Details.Errors);

            // Run a JOIN where an inner query uses a disallowed clause - verify error
            q.Where = QueryParser.Parse("ID > 1 AND ID = #Q2[ID]");
            result = db.Query(q, (si) => false);
            Assert.IsFalse(result.Details.Succeeded);
            Assert.AreEqual("", result.Query.Where.ToString());
            Assert.AreEqual(String.Format(ExecutionDetails.DisallowedColumnQuery, "SecretOwner"), result.Details.Errors);

            // Run a JOIN where no secured columns are accessed - verify success (inner query expanded)
            q.Where = QueryParser.Parse("ID > 1 AND ID = #Q1[ID]");
            result = db.Query(q, (si) => false);
            Assert.IsTrue(result.Details.Succeeded);
            Assert.AreEqual("[ID] > 1 AND [ID] = IN(1)", result.Query.Where.ToString());
        }

        [TestMethod]
        public void SecureDatabase_CustomQuerySecurity()
        {
            // NOTE: Where clause security can be implemented for all queries, but
            // column security is custom per query types. If columns are restricted
            // and the query is an unknown type, it's always blocked.
            SecureDatabase db = BuildSampleDB();

            CustomQuery c = new Model.SecureDatabaseTests.CustomQuery();
            c.Columns = new string[] { "ID" };
            c.TableName = sampleTableName;
            c.Where = QueryParser.Parse("One");

            SelectResult result;

            // Run the Query without security. Expect no restrictions and no security checks.
            result = db.Query(c, (si) => true);
            Assert.IsTrue(result.Details.Succeeded);

            // Restrict the secret columns to people in "G1", or users in "G2" can see everything but only for SecretPriority < 1 items.
            SecureSampleDB(db);

            // Run the Query when the user has permission to everything. Verify success.
            result = db.Query(c, (si) => si.Name == "g1" || si.Name == "g2");
            Assert.IsTrue(result.Details.Succeeded);

            // Run the Query when the user has a row restriction. Verify success with row restrictor.
            result = db.Query(c, (si) => si.Name == "g3");
            Assert.IsTrue(result.Details.Succeeded);
            Assert.AreEqual("[SecretPriority] > 1 AND [*]:One", result.Query.Where.ToString());

            // Run the Query when the user has a column restriction. Verify error.
            result = db.Query(c, (si) => false);
            Assert.IsFalse(result.Details.Succeeded);
            Assert.IsTrue(result.Details.Errors.Contains(String.Format(ExecutionDetails.DisallowedQuery, "CustomQuery")));
        }

        private class CustomQuery : SelectQuery
        { }
    }
}
