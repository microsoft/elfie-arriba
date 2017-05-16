// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    public static class ColumnTests
    {
        public static void ColumnTest_Basics<T>(Func<IColumn<T>> createColumnMethod, T sampleValue, T otherValue)
        {
            // Get a new column
            IColumn<T> column = createColumnMethod();
            AssertConsistent(column);
            Assert.AreEqual(0, column.Count);

            // Resize; verify values have default
            column.SetSize(4);
            AssertConsistent(column);
            Assert.AreEqual(4, column.Count);
            Assert.AreEqual(column.DefaultValue, column[0]);
            Assert.AreEqual(column.DefaultValue, column[1]);
            Assert.AreEqual(column.DefaultValue, column[2]);
            Assert.AreEqual(column.DefaultValue, column[3]);

            // Set new values
            column[1] = sampleValue;
            column[3] = otherValue;
            AssertConsistent(column);
            Assert.AreEqual(4, column.Count);
            Assert.AreEqual(column.DefaultValue, column[0]);
            Assert.AreEqual(sampleValue, column[1]);
            Assert.AreEqual(column.DefaultValue, column[2]);
            Assert.AreEqual(otherValue, column[3]);

            // Verify GetValues
            Array values = column.GetValues(new ushort[] { 3, 0, 1 });
            AssertConsistent(column);
            Assert.AreEqual(otherValue, values.GetValue(0));
            Assert.AreEqual(column.DefaultValue, values.GetValue(1));
            Assert.AreEqual(sampleValue, values.GetValue(2));

            // Round-Trip and re-verify
            using (SerializationContext context = new SerializationContext(new MemoryStream()))
            {
                column.WriteBinary(context);
                context.Stream.Seek(0, SeekOrigin.Begin);

                column.SetSize(0);
                column.ReadBinary(context);
            }

            AssertConsistent(column);
            Assert.AreEqual(4, column.Count);
            Assert.AreEqual(column.DefaultValue, column[0]);
            Assert.AreEqual(sampleValue, column[1]);
            Assert.AreEqual(column.DefaultValue, column[2]);
            Assert.AreEqual(otherValue, column[3]);

            // Clear values
            column.SetSize(0);
            AssertConsistent(column);
            Assert.AreEqual(0, column.Count);

            // Resize for values
            column.SetSize(12);
            AssertConsistent(column);
            Assert.AreEqual(12, column.Count);

            // Column fill tests - these tests ensure column fills across resizes correctly
            column = createColumnMethod();

            // Fill the column up to the first resize (conservative)
            column.SetSize(ArrayExtensions.MinimumSize - 1);
            Assert.AreEqual(column.Count, ArrayExtensions.MinimumSize - 1);
            AssertConsistent(column);

            // Move the column past the first resize
            column.SetSize(ArrayExtensions.MinimumSize + 5);
            Assert.AreEqual(column.Count, ArrayExtensions.MinimumSize + 5);
            AssertConsistent(column);

            // Move the column to max
            column.SetSize(ushort.MaxValue);
            Assert.AreEqual(column.Count, ushort.MaxValue);
            AssertConsistent(column);
        }

        public static string GetSortedValues<T>(IColumn<T> column)
        {
            StringBuilder result = new StringBuilder();

            IList<ushort> sortedIndexes;
            int sortedIndexesCount;

            if (column.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount))
            {
                for (int i = 0; i < column.Count; ++i)
                {
                    result.AppendLine(String.Format("{0}: {1}", sortedIndexes[i], column[sortedIndexes[i]]));
                }
            }

            return result.ToString();
        }

        public static string GetSortedIndexes<T>(IColumn<T> column)
        {
            StringBuilder result = new StringBuilder();

            IList<ushort> sortedIndexes;
            int sortedIndexesCount;
            if (column.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount))
            {
                for (int i = 0; i < column.Count; ++i)
                {
                    if (result.Length > 0) result.Append(", ");
                    result.Append(sortedIndexes[i]);
                }
            }

            return result.ToString();
        }

        public static string GetMatches<T>(IColumn<T> column, Operator op, T value)
        {
            ShortSet result = new ShortSet(column.Count);
            ExecutionDetails details = new ExecutionDetails();

            // Get the matches
            column.TryWhere(op, value, result, details);

            // Return a distinct value if the evaluation was reported unsuccessful
            if (!details.Succeeded) return null;

            // Return matches for successful evaluation
            return String.Join(", ", result.Values);
        }

        public static ushort GetIndex<T>(IColumn<T> column, T value)
        {
            ushort index;
            column.TryGetIndexOf(value, out index);
            return index;
        }

        public static void AssertConsistent(IColumn column)
        {
            if (column is ICommittable) (column as ICommittable).Commit();

            ExecutionDetails verifyDetails = new ExecutionDetails();
            column.VerifyConsistency(VerificationLevel.Full, verifyDetails);
            Assert.IsTrue(verifyDetails.Succeeded, verifyDetails.Errors);
        }
    }
}
