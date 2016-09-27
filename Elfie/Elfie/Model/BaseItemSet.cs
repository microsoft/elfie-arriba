// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

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
    public class BaseItemSet : IBinarySerializable
    {
        protected StringStore Strings;
        private List<IColumn> _columns;

        public BaseItemSet()
        {
            this.Strings = new StringStore();
            this._columns = new List<IColumn>();
        }

        /// <summary>
        ///  Add a column for BaseItemSet to manage.
        /// </summary>
        /// <param name="column">IColumn to add</param>
        protected void AddColumn(IColumn column)
        {
            this._columns.Add(column);
        }

        #region IColumn
        /// <summary>
        ///  Add an item with default values to the set
        /// </summary>
        protected virtual void AddItem()
        {
            foreach (IColumn column in this._columns)
            {
                column.Add();
            }
        }

        /// <summary>
        ///  Clear all structures within this set.
        /// </summary>
        public virtual void Clear()
        {
            this.Strings.Clear();

            foreach (IColumn column in this._columns)
            {
                column.Clear();
            }
        }

        /// <summary>
        ///  Convert this set to immutable 
        /// </summary>
        public virtual void ConvertToImmutable()
        {
            if (this.Strings.ConvertToImmutable())
            {
                foreach (IColumn column in this._columns)
                {
                    column.ConvertToImmutable();
                }
            }
        }

        public void WriteBinary(BinaryWriter w)
        {
            this.ConvertToImmutable();

            this.Strings.WriteBinary(w);

            foreach (IBinarySerializable column in this._columns)
            {
                column.WriteBinary(w);
            }
        }

        public void ReadBinary(BinaryReader r)
        {
            this.Clear();

            this.Strings.ReadBinary(r);

            foreach (IBinarySerializable column in this._columns)
            {
                column.ReadBinary(r);
            }
        }
        #endregion
    }
}
