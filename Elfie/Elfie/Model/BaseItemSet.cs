// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    /// <summary>
    ///  BaseItemSet is a base class for Elfie set structures. It provides the 
    ///  shared StringStore for columns to use, and manages Clear, Add, 
    ///  ConvertToImmutable, and serialization for them.
    ///  
    ///  Descendant constructors must call AddColumn for each column they create.
    /// </summary>
    public abstract class BaseItemSet<T> : IReadOnlyList<T>, IBinarySerializable
    {
        protected StringStore Strings;
        private List<IColumn> _columns;
        private int _count;

        public BaseItemSet()
        {
            this.Strings = new StringStore();
            _columns = new List<IColumn>();
        }

        /// <summary>
        ///  Add a column for BaseItemSet to manage.
        /// </summary>
        /// <param name="column">IColumn to add</param>
        protected void AddColumn(IColumn column)
        {
            _columns.Add(column);
        }

        #region IReadOnlyList<T>
        public int Count
        {
            get { return _count; }
        }

        public abstract T this[int index]
        {
            get;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.GetDefaultEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetDefaultEnumerator();
        }
        #endregion

        #region IColumn
        /// <summary>
        ///  Add an item with default values to the set
        /// </summary>
        public virtual T Add()
        {
            foreach (IColumn column in _columns)
            {
                column.Add();
            }

            _count++;

            return this[_count - 1];
        }

        /// <summary>
        ///  Clear all structures within this set.
        /// </summary>
        public virtual void Clear()
        {
            this.Strings.Clear();

            foreach (IColumn column in _columns)
            {
                column.Clear();
            }

            _count = 0;
        }

        /// <summary>
        ///  Convert this set to immutable 
        /// </summary>
        public virtual void ConvertToImmutable()
        {
            if (this.Strings.ConvertToImmutable())
            {
                foreach (IColumn column in _columns)
                {
                    column.ConvertToImmutable();
                }
            }
        }

        public virtual void WriteBinary(BinaryWriter w)
        {
            this.ConvertToImmutable();

            w.Write(_count);
            this.Strings.WriteBinary(w);

            foreach (IBinarySerializable column in _columns)
            {
                column.WriteBinary(w);
            }
        }

        public virtual void ReadBinary(BinaryReader r)
        {
            this.Clear();

            _count = r.ReadInt32();
            this.Strings.ReadBinary(r);

            foreach (IBinarySerializable column in _columns)
            {
                column.ReadBinary(r);
            }
        }
        #endregion
    }
}
