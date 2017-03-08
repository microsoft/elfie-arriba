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
            _strings = strings;
            _identifiers = new PartialArray<int>();
        }

        public String8 this[int index]
        {
            get { return _strings[_identifiers[index]]; }
            set { _identifiers[index] = _strings.FindOrAddString(value); }
        }

        public int IdentifierFor(int index)
        {
            return _identifiers[index];
        }

        public int Count
        {
            get { return _identifiers.Count; }
        }

        public void Clear()
        {
            _identifiers.Clear();
        }

        public void Add()
        {
            _identifiers.Add();
        }

        public void SetCount(int count)
        {
            _identifiers.SetCount(count);
        }

        public void ConvertToImmutable()
        {
            _strings.ConvertToImmutable(_identifiers);
        }

        public void WriteBinary(BinaryWriter w)
        {
            // StringStore is written separately
            _identifiers.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            // StringStore is read separately
            _identifiers.ReadBinary(r);
        }
    }
}
