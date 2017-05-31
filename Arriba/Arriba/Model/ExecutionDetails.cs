// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Extensions;

namespace Arriba.Model
{
    /// <summary>
    ///  ExecutionDetails contains information about how execution went for a given operation.
    /// </summary>
    public class ExecutionDetails
    {
        // Query Execution errors
        public const string ColumnDoesNotExist = "Column '{0}' does not exist";
        public const string UnableToConvertType = "Unable to convert '{0}' to column '{1}' type {2}";
        public const string ColumnDoesNotSupportOperator = "Unable to evaluate operator '{0}' for column '{1}'";

        // ValidateConsistency errors
        public const string TablePartitionBitsWrong = "Table partition bit count {0} is unexpected for table with {1} bits reported by first partition mask and {2} partitions.";
        public const string PartitionHasNoIDColumn = "Partition has no ID column, but one is required.";
        public const string PartitionSizeIsUnexpected = "Partition '{0}' is size {1:n0}, but expected size is approximately {2:n0}.";
        public const string ColumnDoesNotHaveEnoughValues = "Column '{0}' does not have as many items as it should. Size {1:n0}; internal size {2:n0}.";
        public const string ColumnSizeIsUnexpected = "Column '{0}' is size {1:n0} but the partition is size {2:n0}.";
        public const string ItemInWrongPartition = "Item with ID '{0}', hash '{1:x}' is in the wrong partition; in the partition with mask {2}";
        public const string SortedIdOutOfRange = "Column '{0}' has ID {1:n0} which is out of range; only {2:n0} items are in column.";
        public const string SortedIdAppearsMoreThanOnce = "Column '{0}' has ID {1:n0} in the SortedID list multiple times.";
        public const string SortedValuesNotInOrder = "Column '{0}' item {1:n0} [{2}] appears before item {3:n0} [{4}], but [{2}] > [{4}].";
        public const string SortedColumnMissingIDs = "Column '{0}' is missing sorted IDs for valid items [{1}].";
        public const string ByteBlockColumnBatchOutOfRange = "Column '{0}' item {1:n0} reports it is in batch {2:n0}, but there are only {3:n0} batches.";
        public const string ByteBlockEmptyValueMisrecorded = "Column '{0}' item {1:n0} is zero length but has a position ({2:n0}).";
        public const string ByteBlockHugeValueMisrecorded = "Column '{0}' item {1:n0} is 'oversize' length but has a position ({2:n0}).";
        public const string ByteBlockColumnPositionOutOfRange = "Column '{0}' item {1:n0} reports it is at position {2:n0}, length {3:n0} but containing batch is only length {4:n0}.";
        public const string ByteBlockColumnUnclearedIndexEntry = "Column '{0}' out-of-range ID {1:n0} does not have a cleared index entry. Value '{2}'.";
        public const string WordIndexBlockTooFull = "WordIndex for Column '{0}' is above size limit; it has {1:n0} words.";
        public const string WordIndexBlockSizesMismatch = "WordIndex for Column '{0}' block has {1:n0} words but {2:n0} sets; counts must match.";
        public const string WordIndexInvalidItemID = "WordIndex for Column '{0}' word '{1}' has invalid ID(s) [{2}]";

        // Column Security Errors
        public const string DisallowedColumnQuery = "Access Denied to column '{0}'.";
        public const string DisallowedQuery = "Security not implemented for query '{0}'.";

        public bool Succeeded;
        private HashSet<string> _warnings;
        private HashSet<string> _errors;
        private HashSet<string> _accessDeniedColumns;

        public ExecutionDetails()
        {
            this.Succeeded = true;
        }

        public void AddWarning(string format, params object[] arguments)
        {
            lock (this)
            {
                if (_warnings == null) _warnings = new HashSet<string>();

                if (arguments == null || arguments.Length == 0)
                {
                    _warnings.Add(format);
                }
                else
                {
                    _warnings.Add(StringExtensions.Format(format, arguments));
                }
            }
        }

        public void AddError(string format, params object[] arguments)
        {
            lock (this)
            {
                if (_errors == null) _errors = new HashSet<string>();

                if (arguments == null || arguments.Length == 0)
                {
                    _errors.Add(format);
                }
                else
                {
                    _errors.Add(StringExtensions.Format(format, arguments));
                }

                this.Succeeded = false;
            }
        }

        public void AddDeniedColumn(string columnName)
        {
            lock (this)
            {
                if (_accessDeniedColumns == null) _accessDeniedColumns = new HashSet<string>();
                _accessDeniedColumns.Add(columnName);
            }
        }

        public string Warnings
        {
            get { return (_warnings == null ? String.Empty : String.Join("; ", _warnings)); }
        }

        public string Errors
        {
            get { return (_errors == null ? String.Empty : String.Join("; ", _errors)); }
        }

        public IEnumerable<string> AccessDeniedColumns
        {
            get { return _accessDeniedColumns; }
        }

        public void Merge(ExecutionDetails other)
        {
            if (other == null) return;

            lock (this)
            {
                this.Succeeded &= other.Succeeded;

                if (other._errors != null)
                {
                    if (_errors == null) _errors = new HashSet<string>();
                    _errors.UnionWith(other._errors);
                }

                if (other._warnings != null)
                {
                    if (_warnings == null) _warnings = new HashSet<string>();
                    _warnings.UnionWith(other._warnings);
                }

                if (other._accessDeniedColumns != null)
                {
                    if (_accessDeniedColumns == null) _accessDeniedColumns = new HashSet<string>();
                    _accessDeniedColumns.UnionWith(other._accessDeniedColumns);
                }
            }
        }
    }
}
