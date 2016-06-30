// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Extensions;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    public class BooleanColumn : IColumn<bool>
    {
        public string Name { get; set; }
        public bool DefaultValue { get; private set; }
        private ShortSet _trueItems;

        public BooleanColumn(bool defaultValue)
        {
            this.DefaultValue = defaultValue;

            ushort recommendedSize = (ushort)ArrayExtensions.RecommendedSize(ArrayExtensions.MinimumSize, ArrayExtensions.MinimumSize, ushort.MaxValue);
            _trueItems = new ShortSet(recommendedSize);
            if (defaultValue == true)
            {
                _trueItems.Not();
            }
        }

        public bool this[ushort lid]
        {
            get
            {
                return _trueItems.Contains(lid);
            }
            set
            {
                if (value)
                {
                    _trueItems.Add(lid);
                }
                else
                {
                    _trueItems.Remove(lid);
                }
            }
        }

        public bool TryEvaluate(ushort lid, Operator op, bool value, out bool result)
        {
            bool tValue = _trueItems.Contains(lid);
            return tValue.TryEvaluate(op, value, out result);
        }

        public void TryWhere(Operator op, bool value, ShortSet result, ExecutionDetails details)
        {
            if (result == null) throw new ArgumentNullException("result");

            bool matchesTrue = (value == true);

            switch (op)
            {
                case Operator.Equals:
                case Operator.Matches:
                case Operator.MatchesExact:
                    break;
                case Operator.NotEquals:
                    matchesTrue = !matchesTrue;
                    break;
                default:
                    if (details != null) details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
                    return;
            }

            if (matchesTrue)
            {
                result.Or(_trueItems);
            }
            else
            {
                result.OrNot(_trueItems);
            }
        }

        public bool TryGetIndexOf(bool value, out ushort index)
        {
            // Base column doesn't contain sorting information
            index = ushort.MaxValue;
            return false;
        }

        public Array GetValues(IList<ushort> lids)
        {
            if (lids == null) throw new ArgumentNullException("lids");

            bool[] values = new bool[lids.Count];

            for (ushort i = 0; i < values.Length; ++i)
            {
                values[i] = _trueItems.Contains(lids[i]);
            }

            return values;
        }

        public ushort Count
        {
            get { return _trueItems.Capacity; }
        }

        public void SetSize(ushort size)
        {
            ushort oldSize = this.Count;

            if (size != _trueItems.Capacity)
            {
                // Allocate a new size set and copy all set values
                ShortSet newItems = new ShortSet(size);
                newItems.Or(_trueItems);
                _trueItems = newItems;

                // If the default is true, set new values true initially
                if (this.DefaultValue == true)
                {
                    for (ushort i = oldSize; i < size; ++i)
                    {
                        _trueItems.Add(i);
                    }
                }
            }
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            // Boolean column doesn't contain sorting information
            sortedIndexes = null;
            sortedIndexesCount = 0;
            return false;
        }

        public IColumn InnerColumn
        {
            get { return null; }
        }

        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            // Nothing to check
        }

        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.DefaultValue = context.Reader.ReadBoolean();
            _trueItems.ReadBinary(context);
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Writer.Write(this.DefaultValue);
            _trueItems.WriteBinary(context);
        }
    }
}
