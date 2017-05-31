// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model.Column
{
    public class IpRangeColumn : IColumn<ByteBlock>, ICommittable
    {
        private FastAddSortedColumn<uint> StartAddressColumn { get; set; }
        private FastAddSortedColumn<uint> EndAddressColumn { get; set; }

        public IpRangeColumn()
        {
            this.StartAddressColumn = new FastAddSortedColumn<uint>(new ValueTypeColumn<uint>(0));
            this.EndAddressColumn = new FastAddSortedColumn<uint>(new ValueTypeColumn<uint>(0));
        }

        public void Commit()
        {
            this.StartAddressColumn.Commit();
            this.EndAddressColumn.Commit();
        }

        #region IColumn Simple Passthroughs
        public ByteBlock DefaultValue => "0.0.0.0";

        public string Name
        {
            get { return this.StartAddressColumn.Name; }
            set { this.StartAddressColumn.Name = value; }
        }

        public ushort Count
        {
            get { return this.StartAddressColumn.Count; }
        }

        public IColumn InnerColumn => null;

        public void SetSize(ushort size)
        {
            this.StartAddressColumn.SetSize(size);
            this.EndAddressColumn.SetSize(size);
        }

        public bool TryGetIndexOf(ByteBlock value, out ushort index)
        {
            // We don't implement searching
            index = ushort.MaxValue;
            return false;
        }

        public bool TryEvaluate(ushort lid, Operator op, ByteBlock value, out bool result)
        {
            // We don't implement TryEvaluate
            result = false;
            return false;
        }

        public Array GetValues(IList<ushort> lids)
        {
            ByteBlock[] results = new ByteBlock[lids.Count];

            for (int i = 0; i < lids.Count; ++i)
            {
                results[i] = this[lids[i]];
            }

            return results;
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            // Consider IPs sorted by the start address
            return this.StartAddressColumn.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount);
        }

        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            this.StartAddressColumn.VerifyConsistency(level, details);
            this.EndAddressColumn.VerifyConsistency(level, details);
        }

        public void ReadBinary(ISerializationContext context)
        {
            this.StartAddressColumn.ReadBinary(context);
            this.EndAddressColumn.ReadBinary(context);
        }

        public void WriteBinary(ISerializationContext context)
        {
            this.StartAddressColumn.WriteBinary(context);
            this.EndAddressColumn.WriteBinary(context);
        }
        #endregion

        #region IColumn Complex Members
        public ByteBlock this[ushort lid]
        {
            get { return (new IpRange(this.StartAddressColumn[lid], this.EndAddressColumn[lid]).ToString()); }
            set
            {
                IpRange result;
                IpRange.TryParse(value.ToString(), out result);

                this.StartAddressColumn[lid] = result.StartInclusive;
                this.EndAddressColumn[lid] = result.EndInclusive;
            }
        }

        public void TryWhere(Operator op, ByteBlock value, ShortSet result, ExecutionDetails details)
        {
            // Convert the value to an IP Range. Error if not.
            IpRange valueRange;
            if (!IpRange.TryParse(value.ToString(), out valueRange))
            {
                details.AddError(ExecutionDetails.UnableToConvertType, value, this.Name, "IP Range");
                return;
            }

            if (op == Operator.Matches)
            {
                // Matches finds rows which overlap the passed range.

                // Get rows which *don't overlap* because the start address is after the range being searched for
                this.StartAddressColumn.TryWhere(Operator.GreaterThan, valueRange.EndInclusive, result, details);

                // Add rows which *don't overlap* because the end address is before the range being searched for
                this.EndAddressColumn.TryWhere(Operator.LessThan, valueRange.StartInclusive, result, details);

                // Negate to find the set which *do* overlap
                result.Not();
            }
            else if (op == Operator.Equals || op == Operator.MatchesExact || op == Operator.NotEquals)
            {
                // Equals and MatchExact find rows which exactly equal the range being searched for

                // Find rows with the wrong start
                this.StartAddressColumn.TryWhere(Operator.NotEquals, valueRange.StartInclusive, result, details);

                // Add rows the wrong end
                this.EndAddressColumn.TryWhere(Operator.NotEquals, valueRange.EndInclusive, result, details);

                // Negate to find the set which are equal (both start and end match)
                if (op != Operator.NotEquals)
                {
                    result.Not();
                }
            }
            else if (op == Operator.LessThan)
            {
                // Find rows which end before the start of the search range
                this.EndAddressColumn.TryWhere(Operator.LessThan, valueRange.StartInclusive, result, details);
            }
            else if (op == Operator.GreaterThan)
            {
                // Find rows start after the end of the search range
                this.StartAddressColumn.TryWhere(Operator.GreaterThan, valueRange.EndInclusive, result, details);
            }
            else if (op == Operator.LessThanOrEqual)
            {
                // Find rows which end before the end of the search range
                this.EndAddressColumn.TryWhere(Operator.LessThanOrEqual, valueRange.EndInclusive, result, details);
            }
            else if (op == Operator.GreaterThanOrEqual)
            {
                // Find rows which start after the start of the search range
                this.StartAddressColumn.TryWhere(Operator.GreaterThanOrEqual, valueRange.StartInclusive, result, details);
            }
            else
            {
                details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
            }
        }
        #endregion
    }
}
