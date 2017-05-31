// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Model.Query;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public void Database_Join()
        {
            Database db = new Database();

            Table people = db.AddTable("People", 1000);
            people.AddOrUpdate(new DataBlock(new string[] { "Alias", "Name", "Team", "Groups" }, 3,
                new Array[]
                {
                    new string[] { "mikefan", "rtaket", "v-scolo" },
                    new string[] { "Michael Fanning", "Ryley Taketa", "Scott Louvau"},
                    new string[] { "T1", "T1", "T2" },
                    new string[] { "G1; G2", "G1; G3", "G4" }
                }), new AddOrUpdateOptions() { AddMissingColumns = true });

            Table orders = db.AddTable("Orders", 1000);
            orders.AddOrUpdate(new DataBlock(new string[] { "OrderNumber", "OrderedByAlias" }, 6,
                new Array[]
                {
                    new string[] { "O1", "O2", "O3", "O4", "O5", "O6" },
                    new string[] { "mikefan", "mikefan", "rtaket", "v-scolo", "rtaket", "mikefan; rtaket" }
                }), new AddOrUpdateOptions() { AddMissingColumns = true });

            SelectResult result;

            // Get Orders where OrderedByAlias is any Alias matching "T1" in People (equals JOIN)
            JoinQuery<SelectResult> q = new JoinQuery<SelectResult>(
                db,
                new SelectQuery() { Where = SelectQuery.ParseWhere("OrderedByAlias=#Q1[Alias]"), TableName = "Orders", Columns = new string[] { "OrderNumber" } },
                new SelectQuery() { Where = SelectQuery.ParseWhere("T1"), TableName = "People" }
            );

            q.Correct(null);
            result = db.Query(q);
            Assert.AreEqual("O5, O3, O2, O1", JoinResultColumn(result));

            // Get People where Groups contains a Group Mike is in (contains self JOIN)
            q = new JoinQuery<SelectResult>(
                db,
                new SelectQuery() { Where = SelectQuery.ParseWhere("Groups:#Q1[Groups]"), TableName = "People", Columns = new string[] { "Alias" } },
                new SelectQuery() { Where = SelectQuery.ParseWhere("mikefan"), TableName = "People" }
            );

            q.Correct(null);
            result = db.Query(q);
            Assert.AreEqual("rtaket, mikefan", JoinResultColumn(result));

            // TODO:
            //  Unknown column in Join query
            //  Reference Join query out of range
            //  Join returns nothing
            //  Join returns too many
            //  Select column from Join is unknown
            //  Nested Join
        }

        private string JoinResultColumn(SelectResult result, int columnIndex = 0)
        {
            StringBuilder values = new StringBuilder();
            for (int i = 0; i < result.Values.RowCount; ++i)
            {
                if (values.Length > 0) values.Append(", ");
                values.Append(result.Values[i, columnIndex].ToString());
            }
            return values.ToString();
        }
    }
}
