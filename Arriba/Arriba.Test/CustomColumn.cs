// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Test
{
    public static class CustomColumnSupport
    {
        public static void RegisterCustomColumns()
        {
            ColumnFactory.AddColumnCreator("color", (details, columnComponents, initialCapacity) =>
            {
                IColumn<ColorColumn.ComparableColor> coreColumn = new ColorColumn();
                IUntypedColumn utc = new UntypedColumn<ColorColumn.ComparableColor>(coreColumn);

                utc.Name = details.Name;

                return utc;
            });
        }
    }

    public class ColorTable : Table
    {
        public ColorTable(string name, long requiredItemCount) : base(name, requiredItemCount)
        {
        }

        public void BindColorColumns(string sourceColumn, string targetColorColumn)
        {
            foreach (Partition p in GetPartitions())
            {
                IUntypedColumn srcColumn = p.Columns[sourceColumn];
                IUntypedColumn colorColumn = p.Columns[targetColorColumn];
                (colorColumn.InnerColumn as ColorColumn).LookupColumn = (IColumn<short>)srcColumn.InnerColumn;
            }
        }
    }

    public class ColorColumn : IColumn<ColorColumn.ComparableColor>
    {
        public ColorColumn()
        {
        }

        public IColumn<short> LookupColumn { get; set; }

        public ComparableColor this[ushort lid]
        {
            get { return new ComparableColor() { ColorValue = (Color)(LookupColumn[lid] % (int)Color.Count) }; }
            set { throw new NotImplementedException(); }
        }

        public ushort Count { get; private set; }

        public ComparableColor DefaultValue { get { return new ComparableColor() { ColorValue = Color.None }; } }

        public IColumn InnerColumn { get { return null; } }

        public string Name { get; set; }

        public Array GetValues(IList<ushort> lids)
        {
            if (lids == null)
            {
                throw new ArgumentNullException("lids");
            }

            int count = lids.Count;

            ComparableColor[] result = new ComparableColor[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = this[lids[i]];
            }

            return result;
        }

        public void SetSize(ushort size)
        {
            this.Count = size;
        }

        public bool TryEvaluate(ushort lid, Operator op, ComparableColor value, out bool result)
        {
            ComparableColor itemValue = this[lid];
            return itemValue.TryEvaluate(op, value, out result);
        }

        public bool TryGetIndexOf(ComparableColor value, out ushort index)
        {
            // Base column doesn't contain sorting information
            index = ushort.MaxValue;
            return false;
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            // Base column doesn't contain sorting information
            sortedIndexes = null;
            sortedIndexesCount = 0;
            return false;
        }

        public void TryWhere(Operator op, ComparableColor value, ShortSet result, ExecutionDetails details)
        {
            // Base Column can't identify matches for any operator in bulk efficiently.
            if (details != null)
            {
                details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
            }
        }

        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
        }

        public void ReadBinary(ISerializationContext context)
        {
        }

        public void WriteBinary(ISerializationContext context)
        {
        }

        public enum Color
        {
            None,
            Red,
            Blue,
            Green,
            Count
        }

        public struct ComparableColor : IComparable<ComparableColor>, IEquatable<ComparableColor>, IComparable
        {
            public Color ColorValue;

            public int CompareTo(object obj)
            {
                if (obj is ComparableColor)
                {
                    return this.CompareTo((ComparableColor)obj);
                }

                throw new ArgumentException("obj");
            }

            public int CompareTo(ComparableColor other)
            {
                return this.ColorValue.CompareTo(other.ColorValue);
            }

            public bool Equals(ComparableColor other)
            {
                return this.ColorValue == other.ColorValue;
            }

            public override bool Equals(object obj)
            {
                if (obj is ComparableColor) return this.Equals((ComparableColor)obj);

                return false;
            }

            public static bool operator ==(ComparableColor left, ComparableColor right)
            {
                return left.ColorValue == right.ColorValue;
            }

            public static bool operator !=(ComparableColor left, ComparableColor right)
            {
                return left.ColorValue != right.ColorValue;
            }

            public override int GetHashCode()
            {
                return this.ColorValue.GetHashCode();
            }

            public override string ToString()
            {
                return this.ColorValue.ToString();
            }
        }
    }
}
