// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Structures;

namespace Arriba.Model
{
    public interface ITable
    {
        #region Column Operations
        /// <summary>
        ///  Return the ID Column in this Table
        /// </summary>
        ColumnDetails IDColumn { get; }

        /// <summary>
        ///  Return details of all columns in this Table
        /// </summary>
        ICollection<ColumnDetails> ColumnDetails { get; }

        /// <summary>
        ///  Add a new column with the given type descriptor and default.
        ///  Columns must be added before values can be set on them.
        /// </summary>
        /// <param name="details">Details of the column to add</param>
        void AddColumn(ColumnDetails details);

        /// <summary>
        ///  Add a new column with the given type descriptor and default.
        ///  Columns must be added before values can be set on them.
        /// </summary>
        /// <param name="details">Details of the column to add</param>
        /// <param name="initialCapacity">Initial storage capacity of the column; use to avoid resizes if the item count is known</param>
        void AddColumn(ColumnDetails details, ushort initialCapacity);

        /// <summary>
        ///  Change the type of a column to a new type. Values are copied from the existing
        ///  column to the new one, if value conversion is possible.
        /// </summary>
        /// <param name="details">Details with existing name and new other details</param>
        void AlterColumn(ColumnDetails details);

        /// <summary>
        ///  Remove the column with the given name.
        /// </summary>
        /// <param name="columnName">Name of column to remove.</param>
        void RemoveColumn(string columnName);
        #endregion

        #region Data Operations
        /// <summary>
        ///  Add or Update the given items with the given values. The ID column must be passed
        ///  and must be the first column. If an ID is not known, the item will be added.
        ///  For each item, the value for each column is set to the provided values.
        /// </summary>
        /// <param name="values">Set of Columns and values to add or update</param>
        void AddOrUpdate(DataBlock.ReadOnlyDataBlock values, AddOrUpdateOptions options);

        /// <summary>
        ///  Delete items from this Table which meet the provided criteria.
        /// </summary>
        /// <param name="where">Expression matching items to delete</param>
        /// <returns>Result including number deleted</returns>
        DeleteResult Delete(IExpression where);
        #endregion

        #region Queries
        /// <summary>
        ///  Run the provided query and return a result across this ITable.
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="query">Query to run</param>
        /// <returns>Result for Query across this ITable</returns>
        T Query<T>(IQuery<T> query);
        #endregion

        #region Management
        /// <summary>
        ///  Check the consistency of internal data structures. Used for problem diagnosis.
        ///    'Quick' should run in under 1ms per column (for typical data).
        ///    'Normal' should run in under 10ms per column.
        ///    'Full' should do all available verification.
        /// </summary>
        /// <param name="level">Level of verification to do</param>
        /// <param name="details">Details to which to add any warnings/errors</param>
        void VerifyConsistency(VerificationLevel level, ExecutionDetails details);
        #endregion
    }
}
