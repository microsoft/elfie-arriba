// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Aggregations;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Serialization;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class TableTestsLarge
    {
        [TestCleanup]
        public void TestCleanup()
        {
            ColumnFactory.ResetColumnCreators();
        }

        [TestMethod]
        public void LargeTable_SelectBasic()
        {
            ITable table = CreateLargeTable();
            BuildLargeSampleData(table);

            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID", "AllOnes" };
            query.Count = 5;
            SelectResult result = table.Query(query);

            Assert.AreEqual(100000, (int)result.Total);
        }

        //[TestMethod]
        public void LargeTable_SelectOrderByIsStable()
        {
            // BUG: SelectQuery is not stable at the moment when OrderBy is not a 
            // unique column.  This is because, the data is merged in 
            // arbitrary groups.  Repeated queries will have results based on
            // how the data is merged.
            //
            // Closely related is a problem that Compute does not have any 
            // tie-breaker logic to OrderBy.  The implied secondary ordering is 
            // ID but that isn't considered when Compute obtains the list of 
            // LIDs to return

            ITable table = CreateLargeTable();
            BuildLargeSampleData(table);

            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID" };
            query.Count = 20;
            query.Where = SelectQuery.ParseWhere("Thousands = 1");
            query.OrderByColumn = "Tens";

            const int limit = 10;
            SelectResult[] resultArray = new SelectResult[limit];
            for (int i = 0; i < limit; ++i)
            {
                resultArray[i] = table.Query(query);
            }

            Assert.AreEqual(1000, (int)resultArray[0].Total);

            string expected = GetBlockAsCsv(resultArray[0].Values);
            for (int i = 1; i < limit; ++i)
            {
                AssertBlockEquals(resultArray[i].Values, expected);
            }
        }

        internal static ITable CreateLargeTable()
        {
            ITable table = new Table("Sample", 1000000)
            {
                ParallelOptions = new System.Threading.Tasks.ParallelOptions()
                {
                    MaxDegreeOfParallelism = 4
                }
            };

            return table;
        }

        internal static void BuildLargeSampleData(ITable table)
        {
            const int limit = 100000;
            var seed = Enumerable.Range(0, limit);

            // Define desired columns
            table.AddColumn(new ColumnDetails("ID", "int", -1, "i", true));
            table.AddColumn(new ColumnDetails("AllOnes", "int", 1, "ao", false));
            table.AddColumn(new ColumnDetails("AllEvens", "short", 0, "even", false));
            table.AddColumn(new ColumnDetails("Tens", "int", 0, "tens", false));
            table.AddColumn(new ColumnDetails("Hundreds", "int", 0, "hundreds", false));
            table.AddColumn(new ColumnDetails("Thousands", "int", 0, "thousands", false));

            DataBlock items = new DataBlock(new string[] { "ID", "AllOnes", "AllEvens", "Tens", "Hundreds", "Thousands" }, limit);
            items.SetColumn(0, seed.ToArray());
            items.SetColumn(1, seed.Select(i => 1).ToArray());
            items.SetColumn(2, seed.Select(i => i % 2).ToArray());
            items.SetColumn(3, seed.Select(i => i / 10).ToArray());
            items.SetColumn(4, seed.Select(i => i / 100).ToArray());
            items.SetColumn(5, seed.Select(i => i / 1000).ToArray());

            table.AddOrUpdate(items, new AddOrUpdateOptions());
        }

        private static T FindColumnComponent<T>(IColumn column)
        {
            IColumn current = column;

            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }

                current = (IColumn)current.InnerColumn;
            }

            return default(T);
        }

        private static Dictionary<string, List<ushort>> GetColumnIndexAsDictionary(IColumn<object> column)
        {
            IndexedColumn indexedColumn = FindColumnComponent<IndexedColumn>(column);
            if (indexedColumn == null)
            {
                return null;
            }
            else
            {
                return indexedColumn.ConvertToDictionary();
            }
        }

        private static string GetBlockAsCsv(DataBlock block)
        {
            StringBuilder result = new StringBuilder();

            for (int rowIndex = 0; rowIndex < block.RowCount; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < block.ColumnCount; ++columnIndex)
                {
                    if (columnIndex > 0) result.Append(", ");
                    result.Append(block[rowIndex, columnIndex] ?? "null");
                }

                result.AppendLine();
            }

            return result.ToString();
        }

        private static void AssertBlockEquals(DataBlock block, string expectedValue)
        {
            string actualValue = GetBlockAsCsv(block);

            // Allow extra newlines in values for easier formatting in code.
            Assert.AreEqual(expectedValue.Trim(), actualValue.Trim());
        }
    }
}
