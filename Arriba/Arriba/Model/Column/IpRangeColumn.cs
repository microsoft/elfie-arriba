using Arriba.Structures;
using System;
using System.Collections.Generic;
using Arriba.Model.Expressions;
using Arriba.Serialization;

namespace Arriba.Model.Column
{
    public class IpRangeColumn : IColumn<ByteBlock>
    {
        private IColumn<uint> StartAddressColumn { get; set; }
        private IColumn<uint> EndAddressColumn { get; set; }

        public IpRangeColumn()
        {
            this.StartAddressColumn = new FastAddSortedColumn<uint>(new ValueTypeColumn<uint>(0));
            this.EndAddressColumn = new FastAddSortedColumn<uint>(new ValueTypeColumn<uint>(0));
        }

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

        public ByteBlock DefaultValue => ByteBlock.Zero;

        public string Name
        {
            get { return this.StartAddressColumn.Name; }
            set { this.StartAddressColumn.Name = value; }
        }

        public ushort Count
        {
            get { return this.StartAddressColumn.Count; }
        }

        // ------ Not done
        public IColumn InnerColumn => throw new NotImplementedException();

        public Array GetValues(IList<ushort> lids)
        {
            throw new NotImplementedException();
        }

        public void ReadBinary(ISerializationContext context)
        {
            throw new NotImplementedException();
        }

        public void SetSize(ushort size)
        {
            throw new NotImplementedException();
        }

        public bool TryEvaluate(ushort lid, Operator op, ByteBlock value, out bool result)
        {
            throw new NotImplementedException();
        }

        public bool TryGetIndexOf(ByteBlock value, out ushort index)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            throw new NotImplementedException();
        }

        public void TryWhere(Operator op, ByteBlock value, ShortSet result, ExecutionDetails details)
        {
            throw new NotImplementedException();
        }

        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            throw new NotImplementedException();
        }

        public void WriteBinary(ISerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}
