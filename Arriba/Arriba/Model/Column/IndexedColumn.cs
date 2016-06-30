// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Indexing;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  IndexedColumn wraps an underlying column and adds word indexing of
    ///  the contained items, supporting the Match and MatchExact operators.
    /// </summary>
    /// <typeparam name="T">Type of underlying values indexed</typeparam>
    public class IndexedColumn : BaseColumnWrapper<ByteBlock>, IColumn<ByteBlock>
    {
        internal IWordSplitter Splitter { get; private set; }
        private WordIndex _index;

        public IndexedColumn(IColumn<ByteBlock> column, IWordSplitter splitter) : base(column)
        {
            this.Splitter = splitter;
            _index = new WordIndex(splitter);
        }

        public override ByteBlock this[ushort lid]
        {
            get { return this.Column[lid]; }
            set
            {
                // Get and copy the old value [it may be overwritten in place, so we need a copy]
                ByteBlock oldValue = this.Column[lid].Copy();

                // Update the underlying value
                this.Column[lid] = value;

                // Make values lowercase for indexing [don't mess up casing of passed value]
                ByteBlock newValue = value.Copy();

                oldValue.ToLowerInvariant();
                newValue.ToLowerInvariant();

                // Index the updated value copies
                _index.Index(lid, oldValue, newValue);
            }
        }

        public override void SetSize(ushort size)
        {
            if (size < this.Count)
            {
                // If items are being removed, unindex them
                for (int i = this.Count - 1; i >= size; --i)
                {
                    ByteBlock oldValue = this[(ushort)i].Copy();
                    oldValue.ToLowerInvariant();

                    _index.Index((ushort)i, oldValue, ByteBlock.Zero);
                }
            }

            base.SetSize(size);
        }

        public override void TryWhere(Operator op, ByteBlock value, ShortSet result, ExecutionDetails details)
        {
            // Evaluate Matches and MatchesExact using the index
            if (op == Operator.MatchesExact)
            {
                ByteBlock lower = value.Copy();
                lower.ToLowerInvariant();

                _index.WhereMatchExact(lower, result);
            }
            else if (op == Operator.Matches)
            {
                ByteBlock lower = value.Copy();
                lower.ToLowerInvariant();

                _index.WhereMatches(lower, result);
            }
            else
            {
                // For clauses we don't handle, allow the base column to try to handle it.
                base.TryWhere(op, value, result, details);
            }
        }

        public override void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            base.VerifyConsistency(level, details);
            _index.VerifyConsistency(this, level, details);
        }

        public Dictionary<string, List<ushort>> ConvertToDictionary()
        {
            return _index.ConvertToDictionary();
        }

        public override void ReadBinary(ISerializationContext context)
        {
            base.ReadBinary(context);
            _index.ReadBinary(context);
        }

        public override void WriteBinary(ISerializationContext context)
        {
            base.WriteBinary(context);
            _index.WriteBinary(context);
        }
    }
}
