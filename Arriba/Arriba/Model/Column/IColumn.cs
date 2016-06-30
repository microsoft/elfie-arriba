// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  IColumn is the basic interface which all column types in Arriba
    ///  must implement, and provides operations required to add, update,
    ///  remove, query, and aggregate values. Any class which implements
    ///  these methods may be used (or composed) into a column in Arriba.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IColumn<T> : IBinarySerializable, IColumn
    {
        /// <summary>
        ///  Return the default value for new items in this column.
        /// </summary>
        T DefaultValue { get; }

        /// <summary>
        ///  Get or Set a value in the column, for getting values returned and
        ///  adding or updating item values.
        /// </summary>
        /// <param name="lid">LID of Item</param>
        /// <returns>Value item has for this column</returns>
        T this[ushort lid] { get; set; }

        /// <summary>
        ///  Determine whether a single item matches the provided expression.
        ///  Used to match clauses individually if finding them in bulk isn't
        ///  supported for the given operator.
        /// </summary>
        /// <param name="lid">LID of item to compare</param>
        /// <param name="op">Operator to apply</param>
        /// <param name="value">Value for Operator</param>
        /// <param name="result">True if Item[LID] matches the expression, False otherwise</param>
        /// <returns>True if column supports this operator, False otherwise</returns>
        bool TryEvaluate(ushort lid, Operator op, T value, out bool result);

        /// <summary>
        ///  Return the set of LIDs which match a given condition, for evaluating
        ///  WHERE clauses.
        /// </summary>
        /// <param name="op">Operator to apply</param>
        /// <param name="value">Value for Operator</param>
        /// <param name="result">ShortSet to add all matches to</param>
        /// <param name="details">Other details of execution</param>
        /// <returns>True if column supports returning all IDs matching expression, False otherwise</returns>
        void TryWhere(Operator op, T value, ShortSet result, ExecutionDetails details);

        /// <summary>
        ///  Find the index (LID) of a given item within the column. Used to look up
        ///  the LIDs given the external ID of an item.
        /// </summary>
        /// <param name="value">Value to search for</param>
        /// <param name="index">Index of item with value, ushort.MaxValue if not found</param>
        /// <returns>True if column supports returning index of value, False otherwise</returns>
        bool TryGetIndexOf(T value, out ushort index);
    }

    /// <summary>
    ///  IColumn contains non-type-specific column members. IColumn&lt;T&gt; cannot be
    ///  casted to unless the column type is known, so this interface provides the only
    ///  members available if that type is unknown by the code using the column.
    /// </summary>
    public interface IColumn
    {
        /// <summary>
        ///  Get or set the column name [debuggability]
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///  Return a set of values in a result array.
        /// </summary>
        /// <param name="lids">IDs of items to return, in order</param>
        /// <returns>Array of values for the items in the ID order</returns>
        Array GetValues(IList<ushort> lids);

        /// <summary>
        ///  Get the number of items within this column.
        /// </summary>
        ushort Count { get; }

        /// <summary>
        ///  Resize the internal data structure to store 'size' items, shrinking
        ///  if required.
        /// </summary>
        /// <param name="size">Number of items to resize to store</param>
        void SetSize(ushort size);

        /// <summary>
        ///  Return the LIDs of each item sorted by the value in this column, for
        ///  finding the top subset of matches for a given ORDER BY.
        /// </summary>
        /// <param name="sortedIndexes">All item indexes (LIDs) in order by value ascending</param>
        /// <param name="sortedIndexesCount">The count of valid items in the sortedIndexes list, any indicies above this up to length exist but are invalid</param>
        /// <returns>True if column supports returning sorted indexes, False otherwise</returns>
        bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount);

        /// <summary>
        ///  Return the column that this column is wrapping, if any, otherwise return null.
        /// </summary>
        IColumn InnerColumn { get; }

        /// <summary>
        ///  Check the consistency of internal data structures. Used for problem diagnosis.
        ///    'Quick' should run in under 1ms per column (for typical data).
        ///    'Normal' should run in under 10ms per column.
        ///    'Full' should do all available verification.
        /// </summary>
        /// <param name="level">Level of verification to do</param>
        /// <param name="details">Details to which to add any warnings/errors</param>
        void VerifyConsistency(VerificationLevel level, ExecutionDetails details);
    }

    /// <summary>
    /// Marks a column which can/needs to be committed when changes (adds) occur
    /// </summary>
    public interface ICommittable
    {
        /// <summary>
        /// Commits any pending changes in a column
        /// </summary>
        void Commit();
    }

    public enum VerificationLevel
    {
        Quick = 0,
        Normal = 1,
        Full = 2
    }
}
