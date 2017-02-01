// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arriba.Model;
using Arriba.Model.Aggregations;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class AggregatorTests
    {
        [TestMethod]
        public void Aggregator_UnsignedTypeDetermination()
        {
            // Verify type determination logic in BaseAggregator.
            // We found that (array is int[]) is true for uint[], causing incorrect conversions and exceptions or wrong results.
            CheckTypeDetermination("ulong");
            CheckTypeDetermination("long");
            CheckTypeDetermination("uint");
            CheckTypeDetermination("int");
            CheckTypeDetermination("ushort");
            CheckTypeDetermination("short");
            CheckTypeDetermination("byte");
        }

        [TestMethod]
        public void Aggregator_RequireMerge()
        {
            Table t = new Table("Sample", 100);
            TableTests.AddSampleData(t);

            var aggregator = new AggregatorNeedingMerge();
            var aq = new AggregationQuery();
            aq.Aggregator = aggregator;

            aggregator.RequireMerge = false;
            AggregationResult result = t.Query(aq);
            Assert.AreEqual((int)result.Values[0, 0], 2);

            aggregator.RequireMerge = true;
            result = t.Query(aq);
            Assert.AreEqual((int)result.Values[0, 0], 1);
        }

        private void CheckTypeDetermination(string numericColumnTypeName)
        {
            // Create a numeric column with 0-10 in it
            IUntypedColumn column = ColumnFactory.Build(new ColumnDetails("Unused", numericColumnTypeName, 10), 0);
            column.SetSize(10);
            for (int i = 0; i < 10; ++i)
            {
                column[(ushort)i] = i;
            }

            // Include 0, 2, 4, 6, 8 in the results
            ShortSet matches = new ShortSet(10);
            for (int i = 0; i < 10; i += 2)
            {
                matches.Add((ushort)i);
            }

            // Ask for the Min and verify both the value and type are correct
            // This verifies the type checks in BaseAggregator.Aggregate determine type correctly
            MinAggregator aggregator = new MinAggregator();
            object context = aggregator.CreateContext();
            object result = aggregator.Aggregate(context, matches, new IUntypedColumn[] { column });
            Assert.AreEqual(column[0], result);
        }

        private class AggregatorNeedingMerge : IAggregator
        {
            public bool RequireMerge { get; set; }

            public object CreateContext()
            {
                return (object)1;
            }

            public object Aggregate(object context, ShortSet matches, IUntypedColumn[] columns)
            {
                return (object)2;
            }

            public object Merge(object context, object[] values)
            {
                return context;
            }
        }

        private object[] BuildArray<T>(int length, T first, Func<T, T> next)
        {
            object[] ascending = new object[length];
            ascending[0] = first;

            for (int i = 1; i < length; ++i)
            {
                ascending[i] = next((T)ascending[i - 1]);
            }

            return ascending;
        }

        [TestMethod]
        public void Aggregator_TypeTests()
        {
            // Build a DataBlock with 0-99
            object[] ascending = BuildArray(100, 0, (i) => i + 1);
            int sum = Enumerable.Range(0, 100).Sum();

            // Integer Types
            TestAggregations("long", ascending, sum.ToString());
            TestAggregations("ulong", ascending, sum.ToString());
            TestAggregations("int", ascending, sum.ToString());
            TestAggregations("uint", ascending, sum.ToString());
            TestAggregations("short", ascending, sum.ToString());
            TestAggregations("ushort", ascending, sum.ToString());
            TestAggregations("byte", ascending, sum.ToString());

            // Floating Point Types
            TestAggregations("float", ascending, sum.ToString());
            TestAggregations("double", ascending, sum.ToString());

            // String [ByteBlock]
            TestAggregations("string", ascending, null);

            // Other Types
            TestAggregations("DateTime", BuildArray(100, new DateTime(2016, 01, 01, 0, 0, 0, DateTimeKind.Utc), (t) => t.AddDays(1)), null);
            TestAggregations("TimeSpan", BuildArray(100, TimeSpan.Zero, (t) => TimeSpan.FromMinutes(t.TotalMinutes + 1)), TimeSpan.FromMinutes(sum).ToString());

            int nextGuid = 0;
            TestAggregations("Guid", BuildArray(100, new Guid(nextGuid++, 0, 0, new byte[8]), (g) => new Guid(nextGuid++, 0, 0, new byte[8])), null);
        }

        private void TestAggregations<T>(string columnType, T[] ascending, string sum)
        {
            DataBlock block = new DataBlock(new string[] { "ID" }, ascending.Length);
            block.SetColumn(0, ascending);

            // Add to a table with the desired column type, big enough to have multiple partitions
            Table table = new Table("Sample", 100000);
            table.AddColumn(new ColumnDetails("ID", columnType, null, "", true));
            table.AddOrUpdate(block);

            // Build an AggregationQuery
            AggregationQuery q = new AggregationQuery();
            q.AggregationColumns = new string[] { "ID" };
            AggregationResult result;

            // Verify the Max equals the last value from the array
            q.Aggregator = new MaxAggregator();
            result = table.Query(q);
            Assert.AreEqual(ascending[ascending.Length - 1].ToString(), result.Values[0, 0].ToString());

            // Verify the Min equals the first value from the array
            q.Aggregator = new MinAggregator();
            result = table.Query(q);
            Assert.AreEqual(ascending[0].ToString(), result.Values[0, 0].ToString());

            // Verify the Count equals the rows added
            q.Aggregator = new CountAggregator();
            result = table.Query(q);
            Assert.AreEqual(ascending.Length.ToString(), result.Values[0, 0].ToString());

            // Verify the Sum equals the sum from the array [if computable]
            q.Aggregator = new SumAggregator();
            if (sum == null)
            {
                Verify.Exception<NotImplementedException>(() => table.Query(q));
            }
            else
            {
                result = table.Query(q);
                Assert.AreEqual(sum, result.Values[0, 0].ToString());
            }

            // Verify BaseAggregator throws NotImplemented [the default]
            q.Aggregator = new BaseAggregator();
            Verify.Exception<NotImplementedException>(() => table.Query(q));
        }

        [TestMethod]
        public void Aggregator_BaseBehaviors()
        {
            AggregatorBaseBehaviors(new CountAggregator(), false);
            AggregatorBaseBehaviors(new SumAggregator());
            AggregatorBaseBehaviors(new MinAggregator());
            AggregatorBaseBehaviors(new MaxAggregator());

            // Check BaseAggregator doesn't implement unexpected types or methods
            IUntypedColumn column = ColumnFactory.Build(new ColumnDetails("ID", "bool", false), 100);
            ShortSet sample = new ShortSet(100);
            sample.Or(new ushort[] { 1, 2, 3 });

            IAggregator aggregator = new BaseAggregator();
            Verify.Exception<NotImplementedException>(() => aggregator.Aggregate(null, sample, new IUntypedColumn[] { column }));
            Verify.Exception<NotImplementedException>(() => aggregator.Merge(null, new object[2]));
        }

        private void AggregatorBaseBehaviors(IAggregator aggregator, bool requiresColumns = true)
        {
            // Verify ToString returns the aggregator type, which matches the start of the class name
            string name = aggregator.ToString();
            Assert.AreEqual(aggregator.GetType().Name.ToLowerInvariant(), (name + "aggregator").ToLowerInvariant());

            // Verify Merge throws if the values are null
            Verify.Exception<ArgumentNullException>(() => aggregator.Merge(null, null));

            // Verify Aggregate throws if the matches or columns are null
            Verify.Exception<ArgumentNullException>(() => aggregator.Aggregate(null, null, new IUntypedColumn[1] { ColumnFactory.Build(new ColumnDetails("ID", "int", null), 100) }));

            if (requiresColumns)
            {
                ShortSet sample = new ShortSet(100);
                sample.Or(new ushort[] { 1, 2, 3 });
                Verify.Exception<ArgumentException>(() => aggregator.Aggregate(null, sample, null));
            }
        }
    }
}
