// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  String8Column is an IColumn to make it easy to expose string
    ///  properties from Elfie structures. It references a shared StringStore
    ///  with the strings themselves and the PartialArray pointing to the value
    ///  each item has.
    /// </summary>
    public class String8Column : IColumn
    {
        private StringStore _strings;
        private PartialArray<int> _identifiers;

        public String8Column(StringStore strings)
        {
            this._strings = strings;
            this._identifiers = new PartialArray<int>();
        }

        public String8 this[int index]
        {
            get { return this._strings[this._identifiers[index]]; }
            set { this._identifiers[index] = this._strings.FindOrAddString(value); }
        }

        public int Count
        {
            get { return this._identifiers.Count; }
        }

        public void Clear()
        {
            this._identifiers.Clear();
        }

        public void Add()
        {
            this._identifiers.Add();
        }

        public void ConvertToImmutable()
        {
            this._strings.ConvertToImmutable(this._identifiers);
        }

        public void WriteBinary(BinaryWriter w)
        {
            // StringStore is written separately
            this._identifiers.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            // StringStore is read separately
            this._identifiers.ReadBinary(r);
        }
    }
}
