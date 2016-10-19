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
    public class TableTests
    {
        [TestMethod]
        public void Partition_RoundTrip()
        {
            Partition p = new Partition(PartitionMask.All);
            AddSampleData(p);

            // Round-Trip and re-verify
            Partition p2 = new Partition(PartitionMask.All);

            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                p.WriteBinary(context);
                context.Stream.Seek(0, SeekOrigin.Begin);
                p2.ReadBinary(context);
            }

            // Verify all columns came back
            Assert.AreEqual(String.Join(", ", p.ColumnNames), String.Join(", ", p2.ColumnNames));

            // Select top 3 bugs with Priority = 3 and ID <= 12000, order by ID
            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID", "Priority" };
            query.Count = 3;
            query.Where = SelectQuery.ParseWhere("Priority = 3 AND ID <= 12000");
            SelectResult result = p2.Select(query);
            Assert.AreEqual(2, (int)result.Total);
            Assert.AreEqual("11999", result.Values[0, 0].ToString());
            Assert.AreEqual("11643", result.Values[1, 0].ToString());

            // Verify column details are consistent
            foreach (ColumnDetails cd in p.ColumnDetails)
            {
                ColumnDetails cd2 = p2.DetailsByColumn[cd.Name];
                Assert.AreEqual(cd2.Name, cd.Name);
                Assert.AreEqual(cd2.Type, cd.Type);
                Assert.AreEqual(cd2.Alias, cd.Alias);
                Assert.AreEqual(cd2.IsPrimaryKey, cd.IsPrimaryKey);

                // Verify default values - defaults are serialized as string and null/empty aren't distinguishable, so we have to compare that way as well
                Assert.AreEqual((cd2.Default ?? String.Empty).ToString(), (cd.Default ?? String.Empty).ToString());
            }

            // Verify columns themselves and raw data are consistent
            foreach (IUntypedColumn c in p.Columns.Values)
            {
                IUntypedColumn c2 = p2.Columns[c.Name];
                Assert.AreEqual(c2.Name, c.Name);
                Assert.AreEqual(c2.Count, c.Count);
                Assert.AreEqual(c2.DefaultValue, c.DefaultValue);

                for (ushort i = 0; i < c.Count; ++i)
                {
                    Assert.AreEqual(c2[i], c[i]);
                }
            }
        }

        [TestMethod]
        public void Partition_AddOrUpdate_IncludeOnlyFromArray()
        {
            Partition p = new Partition(PartitionMask.All);
            AddColumns(p);

            // Get sample items but ask the partition only to add some of them
            DataBlock block = BuildSampleData();
            int[] partitionChains = new int[] { 1, -1, 3, 4, -1 };
            int[] partitionChainHeads = new int[] { 0, 2 };
            p.AddOrUpdate(block, new AddOrUpdateOptions(), partitionChains, partitionChainHeads[1]);

            // Verify only the right items were added
            SelectQuery q = new SelectQuery();
            q.Columns = new string[] { "ID" };
            q.Where = new AllExpression();
            q.OrderByColumn = "ID";
            q.OrderByDescending = false;
            SelectResult result = p.Query(q);
            Assert.AreEqual(3, (int)result.Total);
            Assert.AreEqual("11943", result.Values[0, 0].ToString());
            Assert.AreEqual("11999", result.Values[1, 0].ToString());
            Assert.AreEqual("12505", result.Values[2, 0].ToString());
        }

        [TestMethod]
        public void Partition_AddAndUpdateInSameOperation()
        {
            ITable_AddAndUpdateInSameOperation(() =>
            {
                return new Partition(PartitionMask.All);
            });
        }

        [TestMethod]
        public void Partition_All()
        {
            ITable_All(() =>
            {
                return new Partition(PartitionMask.All);
            });
        }

        [TestMethod]
        public void Table_All()
        {
            ITable_All(() => new Table("Sample", 75000));
        }

        [TestMethod]
        public void Table_All_AfterRoundTrip()
        {
            Table t = new Table("Sample", 100000);
            t.Drop();
            t.Save();

            Table t2 = new Table();
            t2.Load("Sample");

            ITable_All(() => t2);
        }

        [TestMethod]
        public void Table_All_RoundTripAfterData()
        {
            Table t = new Table("Sample", 100000);
            AddSampleData(t);
            t.Drop();
            t.Save();

            Table t2 = new Table();
            t2.Load("Sample");
            ITable_All(() => t2, false);
        }

        [TestMethod]
        public void Table_AddOrUpdate_NoAddRows()
        {
            Table t = new Table("Sample", 50000);
            t.AddOrUpdate(BuildSampleData());

            // Add one new item and update an item
            DataBlock newData = new DataBlock(new string[] { "ID", "Title" }, 2,
                new Array[]
                {
                    new int[] { 11512, 12345, 12346 },
                    new string[] { "Existing Item", "New Item", "New Item" }
                });

            // Ask Arriba not to add new items. Verify no items added, existing item updated
            t.AddOrUpdate(newData, new AddOrUpdateOptions() { AddMissingRows = false });
            Assert.AreEqual(5, (int)t.Count);

            SelectQuery q = new SelectQuery() { Columns = new string[] { "Title" }, Count = 10, Where = SelectQuery.ParseWhere("ID = 11512") };
            SelectResult result = t.Select(q);
            Assert.AreEqual("Existing Item", result.Values[0, 0].ToString());
        }

        [TestMethod]
        public void Table_DynamicColumnCreation()
        {
            Table t = new Table("Sample", 50000);
            t.AddOrUpdate(BuildSampleData());

            // When empty, table should automatically add columns
            Assert.AreEqual(5, t.ColumnDetails.Count);

            // Verify expected type inference
            Assert.AreEqual("Int32", t.GetDetails("Priority").Type);
            Assert.AreEqual("String", t.GetDetails("Title").Type);
            Assert.AreEqual("Int32", t.GetDetails("ID").Type);
            Assert.AreEqual("Boolean", t.GetDetails("IsDuplicate").Type);
            Assert.AreEqual("TimeSpan", t.GetDetails("ActiveTime").Type);

            // Identity column should be 'ID', even though not first
            Assert.AreEqual("ID", t.IDColumn.Name);

            // Data should've been added
            Assert.AreEqual(5, (int)t.Count);

            // After the first add, new columns should not be dynamically added
            DataBlock block = new DataBlock(new string[] { "ID", "Priority", "NewColumn" }, 1);
            block.SetRow(0, new object[] { 12345, 1, "New Value" });

            try
            {
                t.AddOrUpdate(block);
                Assert.Fail("Block with new column after first insert shouldn't successfully be added.");
            }
            catch (ArribaException)
            {
                // Expected. An ArribaException should be thrown identifying the first unknown column.
            }

            // Verify add didn't partially happen
            Assert.AreEqual(5, (int)t.Count);

            // Build more data and a new column
            DataBlock newData = new DataBlock(new string[] { "ID", "Resolution" }, 2,
                new Array[]
                {
                    new int[] { 12345, 12346 },
                    new string[] { "Fixed", "Won't Fix" }
                });

            // Verify default AddOrUpdate won't add columns again
            try
            {
                t.AddOrUpdate(newData);
                Assert.Fail("AddOrUpdate shouldn't add columns after the first insert.");
            }
            catch(ArribaException)
            {
                // Expected - columns are only added on the first insert
            }

            // Verify AddOrUpdate with option set will add the new column
            t.AddOrUpdate(newData, new AddOrUpdateOptions() { AddMissingColumns = true });

            Assert.AreEqual(7, (int)t.Count);
        }

        [TestMethod]
        public void Table_DynamicColumnCreation_IdManuallyAdded()
        {
            Table t = new Table("Sample", 50000);

            // Table should allow the primary key column to be defined first
            t.AddColumn(new ColumnDetails("ActiveTime", "TimeSpan", null) { IsPrimaryKey = true });

            t.AddOrUpdate(BuildSampleData());

            // When empty, table should automatically add columns
            Assert.AreEqual(5, t.ColumnDetails.Count);

            // Verify expected type inference
            Assert.AreEqual("Int32", t.GetDetails("Priority").Type);
            Assert.AreEqual("String", t.GetDetails("Title").Type);
            Assert.AreEqual("Int32", t.GetDetails("ID").Type);
            Assert.AreEqual("Boolean", t.GetDetails("IsDuplicate").Type);
            Assert.AreEqual("TimeSpan", t.GetDetails("ActiveTime").Type);

            // Identity column should be 'Priority' because that was specified
            Assert.AreEqual("ActiveTime", t.IDColumn.Name);

            // Data should've been added
            Assert.AreEqual(5, (int)t.Count);
        }

        [TestMethod]
        public void Table_Add_From_Typed_Array()
        {
            const int rangeMax = 1000;

            var IDColumn = new ColumnDetails("ID", "int", null) { IsPrimaryKey = true };
            var ExtraColumn = new ColumnDetails("Extra", "int", null);

            int[] intArray = Enumerable.Range(0, rangeMax).ToArray();
            long[] longArray = Enumerable.Range(0, rangeMax).Select(i => (long)i).ToArray();
            string[] stringArray = longArray.Select(l => l.ToString()).ToArray();

            var columnTypes = new string[] { "int", "long", "double" };
            var sourceTypes = new Tuple<string, Array>[] {
                Tuple.Create<string, Array>("int", intArray),
                Tuple.Create<string, Array>("long", longArray),
                Tuple.Create<string, Array>("string", stringArray)
            };

            // Test for Primary Key
            foreach (string columnType in columnTypes)
            {
                foreach (Tuple<string, Array> sourceType in sourceTypes)
                {
                    string columnName = columnType + "_from_" + sourceType.Item1;

                    var columnDetails = new ColumnDetails(columnName, columnType, null) { IsPrimaryKey = true };

                    Table t = new Table("TEST_" + columnName, rangeMax);
                    t.AddColumn(columnDetails);
                    t.AddColumn(ExtraColumn);

                    DataBlock block = new DataBlock(new string[] { columnName, "Extra" }, rangeMax);
                    block.SetColumn(0, sourceType.Item2);
                    block.SetColumn(1, intArray);

                    t.AddOrUpdate(block);
                    Assert.AreEqual(t.Count, (uint)rangeMax);
                }
            }

            // Test for non-Key
            foreach (string columnType in columnTypes)
            {
                foreach (Tuple<string, Array> sourceType in sourceTypes)
                {
                    string columnName = columnType + "_from_" + sourceType.Item1;

                    var columnDetails = new ColumnDetails(columnName, columnType, null);

                    Table t = new Table("TEST_nonkey_" + columnName, rangeMax);
                    t.AddColumn(IDColumn);
                    t.AddColumn(columnDetails);

                    DataBlock block = new DataBlock(new string[] { "ID", columnName }, rangeMax);
                    block.SetColumn(0, intArray);
                    block.SetColumn(1, sourceType.Item2);

                    t.AddOrUpdate(block);
                    Assert.AreEqual(t.Count, (uint)rangeMax);
                }
            }
        }

        [TestMethod]
        public void Table_AddOrUpdate_DataBlockTooManyRows()
        {
            Table t = new Table("Sample", 1000);

            // Note: DataBlock reports four rows but has five
            DataBlock b = new DataBlock(new string[] { "ID", "Priority", "Title" }, 4,
                new Array[]
                {
                    new int[] { 12345, 12346, 12347, 12348, 12349 },
                    new int[] { 1, 2, 2, 3, 3 },
                    new string[] { "One", "Two", "Three", "Four", "Five" }
                });

            // Verify DataBlock reports four rows
            Assert.AreEqual(4, b.RowCount);

            // Verify Table only inserts rows which are "official"
            t.AddOrUpdate(b);
            Assert.AreEqual(4, (int)t.Count);
        }

        public void ITable_All(Func<ITable> factoryMethod, bool isEmpty = true)
        {
            if (isEmpty)
            {
                ITable_Empty(factoryMethod);
            }

            ITable_Basic(factoryMethod);
            ITable_TypesCheck(factoryMethod);
            ITable_ComplexAndOr(factoryMethod);
            ITable_Distinct(factoryMethod);
            ITable_Aggregate_Count(factoryMethod);
            ITable_Aggregate_Sum(factoryMethod);
            ITable_Aggregate_Min(factoryMethod);
            ITable_Aggregate_Max(factoryMethod);
            ITable_ColumnManagement(factoryMethod);
        }

        public void ITable_Empty(Func<ITable> factoryMethod)
        {
            // Define columns and but DO NOT add sample data
            ITable table = factoryMethod();
            AddColumns(table);

            // Verify select sorted column search returns nothing
            SelectQuery selectQuery = new SelectQuery();
            selectQuery.Columns = new string[] { "ID", "Priority" };
            selectQuery.Count = 3;
            selectQuery.Where = SelectQuery.ParseWhere("ID > -1");
            SelectResult selectResult = table.Select(selectQuery);
            Assert.AreEqual(0, (int)selectResult.Total);

            // Verify select word match returns nothing
            selectQuery.Where = SelectQuery.ParseWhere("Title:One");
            selectResult = table.Select(selectQuery);
            Assert.AreEqual(0, (int)selectResult.Total);

            // Verify count returns 0
            AggregationQuery aggregateQuery = new AggregationQuery();
            aggregateQuery.Aggregator = new CountAggregator();
            aggregateQuery.AggregationColumns = new string[] { "ID" };
            AggregationResult aggregateResult = table.Query(aggregateQuery);
            Assert.AreEqual((ulong)0, aggregateResult.Values[0, 0]);

            // Verify sum returns 0
            aggregateQuery.Aggregator = new SumAggregator();
            aggregateResult = table.Query(aggregateQuery);
            Assert.AreEqual((long)0, aggregateResult.Values[0, 0]);

            // Verify Min/Max return null
            aggregateQuery.Aggregator = new MinAggregator();
            aggregateResult = table.Query(aggregateQuery);
            Assert.IsNull(aggregateResult.Values[0, 0]);

            aggregateQuery.Aggregator = new MaxAggregator();
            aggregateResult = table.Query(aggregateQuery);
            Assert.IsNull(aggregateResult.Values[0, 0]);
        }

        public void ITable_Basic(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            ExecutionDetails details = new ExecutionDetails();
            table.VerifyConsistency(VerificationLevel.Full, details);
            Assert.IsTrue(details.Succeeded);

            // Select top 3 bugs with Priority = 3 and ID <= 12000, order by ID
            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID", "Priority" };
            query.Count = 3;
            query.Where = SelectQuery.ParseWhere("Priority = 3 AND ID <= 12000");
            SelectResult result = table.Select(query);
            Assert.AreEqual(2, (int)result.Total);
            Assert.AreEqual("11999", result.Values[0, 0].ToString());
            Assert.AreEqual("11643", result.Values[1, 0].ToString());

            // Select a second page (not enough items)
            query.Skip = 50;
            result = table.Select(query);
            Assert.AreEqual(2, (int)result.Total);
            Assert.AreEqual(0, (int)result.CountReturned);
            query.Skip = 0;

            // Select a word in first item (cover a simple search and zero-LID handling)
            SelectQuery q2 = new SelectQuery();
            q2.Columns = new string[] { "ID" };
            q2.Count = 100;
            q2.Where = SelectQuery.ParseWhere("Title:One");
            SelectResult r2 = table.Select(q2);
            Assert.AreEqual(1, (int)r2.Total);
            Assert.AreEqual("11512", r2.Values[0, 0].ToString());

            // Select not empty string
            SelectQuery q3 = new SelectQuery(new string[] { "ID" }, "Title != \"\"");
            q3.Count = 100;
            SelectResult r3 = table.Select(q3);
            Assert.AreEqual(5, (int)r3.Total);

            // Update an item and verify it
            DataBlock updateItems = new DataBlock(new string[] { "ID", "Priority" }, 1);
            updateItems.SetRow(0, new object[] { 11643, 2 });
            table.AddOrUpdate(updateItems, new AddOrUpdateOptions());
            result = table.Select(query);
            Assert.AreEqual(1, (int)result.Total);
            Assert.AreEqual("11999", result.Values[0, 0].ToString());

            // Delete an item and verify it
            DeleteResult d1 = table.Delete(SelectQuery.ParseWhere("ID = 11999 OR ID = 11512"));
            Assert.AreEqual(2, (int)d1.Count);
            result = table.Select(query);
            Assert.AreEqual(0, (int)result.Total);

            query.Where = SelectQuery.ParseWhere("ID > 0");
            result = table.Select(query);
            Assert.AreEqual(3, (int)result.Total);

            details = new ExecutionDetails();
            table.VerifyConsistency(VerificationLevel.Full, details);
            Assert.IsTrue(details.Succeeded);
        }

        public void ITable_TypesCheck(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID" };
            query.Count = 10;
            query.OrderByColumn = "ID";
            query.OrderByDescending = false;
            SelectResult result = null;

            // Select bugs with false IsDuplicate
            query.Where = SelectQuery.ParseWhere("IsDuplicate = false");
            result = table.Select(query);
            Assert.AreEqual("11512, 12505", result.Values.GetColumn(0).Join(", "));

            // Select bugs with false (not true) IsDuplicate
            query.Where = SelectQuery.ParseWhere("IsDuplicate != true");
            result = table.Select(query);
            Assert.AreEqual("11512, 12505", result.Values.GetColumn(0).Join(", "));

            // Select bugs with ActiveTime over an hour
            query.Where = SelectQuery.ParseWhere("ActiveTime > 01:00:00");
            result = table.Select(query);
            Assert.AreEqual("11512, 11643, 11943", result.Values.GetColumn(0).Join(", "));
        }

        public void ITable_ComplexAndOr(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID", "Priority" };
            query.Count = 100;

            SelectResult result = null;

            // Verify OR works with more than two clauses
            query.Where = new OrExpression(new TermExpression("Priority", Operator.Equals, 0), new TermExpression("Priority", Operator.Equals, 1), new TermExpression("Priority", Operator.Equals, 3));
            result = table.Select(query);
            Assert.AreEqual(5, (int)result.Total);

            // Verify AND works with more than two clauses
            query.Where = new AndExpression(new TermExpression("Title", Operator.Matches, "Sample"), new TermExpression("Priority", Operator.Equals, 3), new TermExpression("ID", Operator.LessThan, 12000));
            result = table.Select(query);
            Assert.AreEqual(2, (int)result.Total);

            // Bug Coverage: Ensure AND stays empty with multiple clauses
            query.Where = new AndExpression(new TermExpression("Priority", Operator.Equals, 3), new TermExpression("Priority", Operator.Equals, 2), new TermExpression("Priority", Operator.Equals, 3));
            result = table.Select(query);
            Assert.AreEqual(0, (int)result.Total);
        }

        public void ITable_Distinct(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            // Get Distinct Priority for all bugs, verify three (0, 1, 3)
            DistinctQuery query = new DistinctQuery("Priority", "", 5);
            DistinctResult result = table.Query(query);

            // sort the results because order is not garenteed by distinct query
            Array values = result.Values.GetColumn(0);
            Array.Sort(values);

            Assert.AreEqual("0, 1, 3", values.Join(", "));
            Assert.IsTrue(result.AllValuesReturned);

            // Verify the result converts to a dimension properly
            AggregationDimension dimension = result.ToAggregationDimension();
            Assert.AreEqual("Query [Priority = 0,Priority = 1,Priority = 3]", dimension.ToString());

            // Verify distinct priority where priority is not 1 has only two values
            query.Where = QueryParser.Parse("Priority != 1");
            result = table.Query(query);

            // sort the results because order is not garenteed by distinct query
            values = result.Values.GetColumn(0);
            Array.Sort(values);

            Assert.AreEqual("0, 3", result.Values.GetColumn(0).Join(", "));
            Assert.IsTrue(result.AllValuesReturned);

            // Verify if we only ask for one value, query reports more values left
            query.Count = 1;
            result = table.Query(query);

            // either value could come back, it's a race to whichever partition completes first
            Assert.IsTrue(
                ("0" == result.Values.GetColumn(0).Join(", ")) ||
                ("3" == result.Values.GetColumn(0).Join(", ")));

            Assert.IsFalse(result.AllValuesReturned);
        }

        public void ITable_Aggregate_Count(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            AggregationQuery query = new AggregationQuery();
            AggregationResult result;

            query.Aggregator = new CountAggregator();
            query.AggregationColumns = new string[] { "ID" };
            query.Where = SelectQuery.ParseWhere("ID < 12000");

            // Dimensionless Aggregation - verify four bugs aggregated, one cell returned with sum of IDs
            result = table.Query(query);
            Assert.AreEqual((ulong)4, result.Values[0, 0]);

            // One dimension - verify values split out and are correct; dimension queries echoed
            query.Dimensions.Add(new AggregationDimension("Priority", "Priority < 2", "Priority >= 2"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, 2
Priority >= 2, 2
, 4");
            Assert.AreEqual((ulong)2, result.Values[0, 1]);
            Assert.AreEqual((ulong)2, result.Values[1, 1]);
            Assert.AreEqual((ulong)4, result.Values[2, 1]);

            // Two dimensions - verify values split on both; values are correct
            query.Dimensions.Add(new AggregationDimension("Title", "Title:One | Title:Two", "-(Title:One | Title:Two)"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, (Title:One OR Title:Two), 1
Priority < 2, NOT((Title:One OR Title:Two)), 1
Priority < 2, , 2
Priority >= 2, (Title:One OR Title:Two), 1
Priority >= 2, NOT((Title:One OR Title:Two)), 1
Priority >= 2, , 2
, (Title:One OR Title:Two), 2
, NOT((Title:One OR Title:Two)), 2
, , 4
");

            // Sparse dimensions - verify skipping is correct
            query.Dimensions.Clear();
            query.Dimensions.Add(new AggregationDimension("Priority", "Priority = 0", "Priority = 1", "Priority = 2", "Priority = 3"));
            query.Dimensions.Add(new AggregationDimension("ID", "ID < 11900", "ID >= 11900"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority = 0, ID < 11900, 1
Priority = 0, ID >= 11900, null
Priority = 0, , 1
Priority = 1, ID < 11900, null
Priority = 1, ID >= 11900, 1
Priority = 1, , 1
Priority = 2, ID < 11900, null
Priority = 2, ID >= 11900, null
Priority = 2, , null
Priority = 3, ID < 11900, 1
Priority = 3, ID >= 11900, 1
Priority = 3, , 2
, ID < 11900, 2
, ID >= 11900, 2
, , 4
");
            // Add an empty cell and re-check
            query.Dimensions.Clear();
            query.Dimensions.Add(new AggregationDimension("Title", "Title:unused"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Title:unused, null
, 4
");
        }

        public void ITable_Aggregate_Sum(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            AggregationQuery query = new AggregationQuery();
            AggregationResult result;

            query.Aggregator = new SumAggregator();
            query.AggregationColumns = new string[] { "ID" };
            query.Where = SelectQuery.ParseWhere("ID < 12000");

            // Dimensionless Aggregation - verify four bugs aggregated, one cell returned with sum of IDs
            result = table.Query(query);
            Assert.AreEqual(4, (int)result.Total);
            Assert.AreEqual(1, result.Values.ColumnCount);
            Assert.AreEqual(1, result.Values.RowCount);
            Assert.AreEqual((long)47097, result.Values[0, 0]);

            // One dimension - verify values split out and are correct; dimension queries echoed
            query.Dimensions.Add(new AggregationDimension("Priority", "Priority < 2", "Priority >= 2"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, 23455
Priority >= 2, 23642
, 47097
");

            // Two dimensions - verify values split on both; values are correct
            query.Dimensions.Add(new AggregationDimension("Title", "Title:One | Title:Two", "-(Title:One | Title:Two)"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, (Title:One OR Title:Two), 11512
Priority < 2, NOT((Title:One OR Title:Two)), 11943
Priority < 2, , 23455
Priority >= 2, (Title:One OR Title:Two), 11643
Priority >= 2, NOT((Title:One OR Title:Two)), 11999
Priority >= 2, , 23642
, (Title:One OR Title:Two), 23155
, NOT((Title:One OR Title:Two)), 23942
, , 47097
");

            // Add an empty cell and re-check
            query.Dimensions.Clear();
            query.Dimensions.Add(new AggregationDimension("Title", "Title:unused"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Title:unused, null
, 47097
");
        }

        public void ITable_Aggregate_Min(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            AggregationQuery query = new AggregationQuery();
            AggregationResult result;

            query.Aggregator = new MinAggregator();
            query.AggregationColumns = new string[] { "ID" };
            query.Where = SelectQuery.ParseWhere("ID < 12000");

            // Dimensionless Aggregation - verify four bugs aggregated, one cell returned with sum of IDs
            result = table.Query(query);
            Assert.AreEqual(11512, result.Values[0, 0]);

            // One dimension - verify values split out and are correct; dimension queries echoed
            query.Dimensions.Add(new AggregationDimension("Priority", "Priority < 2", "Priority >= 2"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, 11512
Priority >= 2, 11643
, 11512
");

            // Two dimensions - verify values split on both; values are correct
            query.Dimensions.Add(new AggregationDimension("Title", "Title:One | Title:Two", "-(Title:One | Title:Two)"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, (Title:One OR Title:Two), 11512
Priority < 2, NOT((Title:One OR Title:Two)), 11943
Priority < 2, , 11512
Priority >= 2, (Title:One OR Title:Two), 11643
Priority >= 2, NOT((Title:One OR Title:Two)), 11999
Priority >= 2, , 11643
, (Title:One OR Title:Two), 11512
, NOT((Title:One OR Title:Two)), 11943
, , 11512
");

            // Add an empty cell and re-check
            query.Dimensions.Clear();
            query.Dimensions.Add(new AggregationDimension("Title", "Title:unused"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Title:unused, null
, 11512");
        }

        public void ITable_Aggregate_Max(Func<ITable> factoryMethod)
        {
            // Define columns and add sample data
            ITable table = factoryMethod();
            AddSampleData(table);

            AggregationQuery query = new AggregationQuery();
            AggregationResult result;

            query.Aggregator = new MaxAggregator();
            query.AggregationColumns = new string[] { "ID" };
            query.Where = SelectQuery.ParseWhere("ID < 12000");

            // Dimensionless Aggregation - verify four bugs aggregated, one cell returned with sum of IDs
            result = table.Query(query);
            Assert.AreEqual(11999, result.Values[0, 0]);

            // One dimension - verify values split out and are correct; dimension queries echoed
            query.Dimensions.Add(new AggregationDimension("Priority", "Priority < 2", "Priority >= 2"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, 11943
Priority >= 2, 11999
, 11999
");

            // Two dimensions - verify values split on both; values are correct
            query.Dimensions.Add(new AggregationDimension("Title", "Title:One | Title:Two", "-(Title:One | Title:Two)"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Priority < 2, (Title:One OR Title:Two), 11512
Priority < 2, NOT((Title:One OR Title:Two)), 11943
Priority < 2, , 11943
Priority >= 2, (Title:One OR Title:Two), 11643
Priority >= 2, NOT((Title:One OR Title:Two)), 11999
Priority >= 2, , 11999
, (Title:One OR Title:Two), 11643
, NOT((Title:One OR Title:Two)), 11999
, , 11999
");

            // Add an empty cell and re-check
            query.Dimensions.Clear();
            query.Dimensions.Add(new AggregationDimension("Title", "Title:unused"));
            result = table.Query(query);
            AssertBlockEquals(result.Values, @"
Title:unused, null
, 11999");
        }

        private void ITable_AddAndUpdateInSameOperation(Func<ITable> factoryMethod)
        {
            ITable table = factoryMethod();
            AddSampleData(table);

            DataBlock items = new DataBlock(new string[] { "Priority", "Title", "ID", "IsDuplicate", "ActiveTime" }, 4);

            // Update of an existing item
            items.SetRow(0, new object[] { 5, "Modified Existing", 11512, false, 3 });

            // Add a new item
            items.SetRow(1, new object[] { 1, "Newly Added Item", 99998, false, 3 });

            // Add and modify an item in the same operation
            items.SetRow(2, new object[] { 1, "Newly Added - should be modified", 99999, false, 3 });
            items.SetRow(3, new object[] { 2, "Modified Added", 99999, false, 3 });

            table.AddOrUpdate(items, new AddOrUpdateOptions());

            SelectQuery query = new SelectQuery();
            query.Columns = new string[] { "ID", "Priority", "Title" };
            query.Count = ushort.MaxValue;
            query.OrderByColumn = "ID";
            SelectResult result = table.Select(query);

            Assert.AreEqual(7, (int)result.Total);

            Assert.AreEqual("11512", result.Values[0, 0].ToString());
            Assert.AreEqual("5", result.Values[0, 1].ToString());
            Assert.AreEqual("Modified Existing", result.Values[0, 2].ToString());

            Assert.AreEqual("99998", result.Values[5, 0].ToString());
            Assert.AreEqual("1", result.Values[5, 1].ToString());
            Assert.AreEqual("Newly Added Item", result.Values[5, 2].ToString());

            Assert.AreEqual("99999", result.Values[6, 0].ToString());
            Assert.AreEqual("2", result.Values[6, 1].ToString());
            Assert.AreEqual("Modified Added", result.Values[6, 2].ToString());
        }

        private void ITable_ColumnManagement(Func<ITable> factoryMethod)
        {
            ITable table = factoryMethod();
            AddSampleData(table);

            // Add a new column with a non-null default
            string newColumnName = "Area Path";
            string newColumnInitialDefault = @"5";
            table.AddColumn(new ColumnDetails(newColumnName, "string", newColumnInitialDefault));

            // Verify all rows have the new default
            SelectQuery query = new SelectQuery(new string[] { "ID", newColumnName }, "");
            SelectResult result = table.Select(query);

            Assert.AreEqual(5, (int)result.Total);
            for (int i = 0; i < result.Values.RowCount; ++i)
            {
                Assert.AreEqual(newColumnInitialDefault, result.Values[i, 1].ToString());
            }

            // Change the column type (string to ushort)
            ushort newColumnSecondDefault = 10;
            table.AlterColumn(new ColumnDetails(newColumnName, "ushort", newColumnSecondDefault));

            // Verify existing values have been re-typed
            result = table.Select(query);
            Assert.AreEqual(5, (int)result.Total);
            for (int i = 0; i < result.Values.RowCount; ++i)
            {
                Assert.AreEqual((ushort)5, result.Values[i, 1]);
            }

            // Verify column knows the new default
            DataBlock items = new DataBlock(new string[] { "ID" }, 1);
            items[0, 0] = 12345;
            table.AddOrUpdate(items, new AddOrUpdateOptions());

            query = new SelectQuery(query.Columns, "ID = 12345");
            result = table.Select(query);
            Assert.AreEqual(1, (int)result.Total);
            Assert.AreEqual(newColumnSecondDefault, result.Values[0, 1]);

            // Delete the column
            table.RemoveColumn(newColumnName);

            foreach (ColumnDetails cd in table.ColumnDetails)
            {
                if (cd.Name.Equals(newColumnName)) Assert.Fail("Column not removed after RemoveColumn() called.");
            }
        }

        internal static void AddColumns(ITable table)
        {
            // Define desired columns
            table.AddColumn(new ColumnDetails("ID", "int", -1, "i", true));
            table.AddColumn(new ColumnDetails("Title", "string", null, "t", false));
            table.AddColumn(new ColumnDetails("Priority", "short", -1, "p", false));
            table.AddColumn(new ColumnDetails("Created Date", "DateTime", DateTime.MinValue.ToUniversalTime(), "cd", false));
            table.AddColumn(new ColumnDetails("Primary Bug ID", "int", -1, "pid", false));

            table.AddColumn(new ColumnDetails("IsDuplicate", "bool", true, "", false));
            table.AddColumn(new ColumnDetails("ActiveTime", "TimeSpan", null, "at", false));
        }

        internal static DataBlock BuildSampleData()
        {
            DataBlock items = new DataBlock(new string[] { "Priority", "Title", "ID", "IsDuplicate", "ActiveTime" }, 5);
            items.SetColumn(0, new object[] { 0, 3, 1, 3, 3 });
            items.SetColumn(1, new object[] { "Sample One", "Sample Two", "Sample Three", "Sample Four", "Sample Five" });
            items.SetColumn(2, new object[] { 11512, 11643, 11943, 11999, 12505 });
            items.SetColumn(3, new object[] { false, null, "true", true, "False" });
            items.SetColumn(4, new object[] { TimeSpan.FromDays(1.5), "2", "2.12:00:00", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(4) });
            return items;
        }

        internal static void AddSampleData(ITable table)
        {
            // Add the columns (if not already added)
            AddColumns(table);

            // Add some sample data (with ID column NOT first)
            DataBlock items = BuildSampleData();
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

        private static string GetBlockAsCsv(DataBlock aggregateBlock)
        {
            StringBuilder result = new StringBuilder();

            for (int rowIndex = 0; rowIndex < aggregateBlock.RowCount; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < aggregateBlock.ColumnCount; ++columnIndex)
                {
                    if (columnIndex > 0) result.Append(", ");
                    result.Append(aggregateBlock[rowIndex, columnIndex] ?? "null");
                }

                result.AppendLine();
            }

            return result.ToString();
        }

        private static void AssertBlockEquals(DataBlock aggregateBlock, string expectedValue)
        {
            string actualValue = GetBlockAsCsv(aggregateBlock);

            // Allow extra newlines in values for easier formatting in code.
            Assert.AreEqual(expectedValue.Trim(), actualValue.Trim());
        }
    }
}
