using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Model.Query;
using Arriba.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arriba.Test.Model
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public void Database_Join()
        {
            Database d = new Database();
            Table people = d.AddTable("People", 1000);
            people.AddOrUpdate(new DataBlock(new string[] { "Alias", "Name", "Team" }, 3,
                new Array[]
                {
                    new string[] { "mikefan", "rtaket", "v-scolo" },
                    new string[] { "Michael Fanning", "Ryley Taketa", "Scott Louvau"},
                    new string[] { "T1", "T1", "T2" }
                }), new AddOrUpdateOptions() { AddMissingColumns = true });

            Table orders = d.AddTable("Orders", 1000);
            orders.AddOrUpdate(new DataBlock(new string[] { "OrderNumber", "OrderedByAlias" }, 5,
                new Array[]
                {
                    new string[] { "O1", "O2", "O3", "O4", "O5" },
                    new string[] { "mikefan", "mikefan", "rtaket", "v-scolo", "rtaket" }
                }), new AddOrUpdateOptions() { AddMissingColumns = true });

            
            // Ask for Orders where the Alias is in the joined query
            SelectQuery q = new SelectQuery() { Where = SelectQuery.ParseWhere("OrderedByAlias=#Q1[Alias]"), TableName = "Orders" };

            // Ask for People in Team T1
            List<SelectQuery> querySet = new List<SelectQuery>();
            querySet.Add(new SelectQuery() { Where = SelectQuery.ParseWhere("T1"), TableName = "People" });

            // Correct the query - should convert to OrderedByAlias IN (rtaket, mikefan)
            JoinCorrector j = new JoinCorrector(d, null, querySet);
            q.Correct(j);
            Assert.AreEqual("OrderedByAlias = IN(rtaket, mikefan)", q.Where.ToString());

            // Ask for the outer query result - should get all except O4
            SelectResult result = d[q.TableName].Select(q);
            Assert.AreEqual(4, (int)result.Total);

            // TODO:
            //  Unknown column in Join query
            //  Reference Join query out of range
            //  Join returns nothing
            //  Join returns too many
            //  Select column from Join is unknown
            //  Join on 'contains' (rather than equals)
            //  Nested Join
        }
    }
}
