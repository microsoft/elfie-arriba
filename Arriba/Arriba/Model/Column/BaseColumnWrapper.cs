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
    ///  BaseColumnWrapper provides a base class for columns which just wrap
    ///  other columns, delegating calls to the base column. Wrappers can
    ///  then override what they add functionality for only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseColumnWrapper<T> : IColumn<T>, ICommittable
    {
        protected IColumn<T> Column;

        public BaseColumnWrapper(IColumn<T> column)
        {
            this.Column = column;
        }

        public virtual T DefaultValue
        {
            get { return this.Column.DefaultValue; }
        }

        public virtual T this[ushort lid]
        {
            get { return this.Column[lid]; }
            set { this.Column[lid] = value; }
        }

        public virtual Array GetValues(IList<ushort> lids)
        {
            return this.Column.GetValues(lids);
        }

        public string Name
        {
            get { return this.Column.Name; }
            set { this.Column.Name = value; }
        }

        public virtual ushort Count
        {
            get { return this.Column.Count; }
        }

        public virtual void SetSize(ushort size)
        {
            this.Column.SetSize(size);
        }

        public virtual bool TryEvaluate(ushort lid, Operator op, T value, out bool result)
        {
            return this.Column.TryEvaluate(lid, op, value, out result);
        }

        public virtual void TryWhere(Operator op, T value, ShortSet result, ExecutionDetails details)
        {
            this.Column.TryWhere(op, value, result, details);
        }

        public virtual bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            return this.Column.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount);
        }

        public virtual bool TryGetIndexOf(T value, out ushort index)
        {
            return this.Column.TryGetIndexOf(value, out index);
        }

        public virtual void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            this.Column.VerifyConsistency(level, details);
        }

        public virtual void ReadBinary(ISerializationContext context)
        {
            this.Column.ReadBinary(context);
        }

        public virtual void WriteBinary(ISerializationContext context)
        {
            this.Column.WriteBinary(context);
        }

        public IColumn InnerColumn
        {
            get { return this.Column; }
        }

        public virtual void Commit()
        {
            if (Column is ICommittable)
            {
                (Column as ICommittable).Commit();
            }
        }
    }
}
