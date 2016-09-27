// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test
{
    public enum SampleItemType : byte
    {
        Unknown = 0,
        Basic = 1,
        Complex = 2
    }

    /// <summary>
    ///  SampleItem is an example Elfie data structure.
    ///  
    ///  Normally, C# data structures are classes with fields or properties.
    ///  These are easy to use, but create object overhead [24b/item] and allocate for every item, which is expensive for large sets.
    ///  Each property of each item is serialized individually, so serialization speeds are limited.
    ///  
    ///  One can also use structs to avoid per-object overhead.
    ///  These are weird to use - they are fully copied when passed to methods and must be copied back to be "changed".
    ///  Each property of structs must also be serialized individually.
    ///  
    ///  The Elfie design is different.
    ///  Values are stored in arrays in a set class, rather than in the items. [Structure of Arrays pattern].
    ///  Properties are therefore serialized a whole column at a time, which is dramatically faster.
    ///  There is a struct to represent the item in code, but it only contains a pointer to the set and an index.
    ///  The item struct exposes properties which get and set values from the right index in the arrays in the set.
    ///  
    ///  The Elfie design:
    ///   - Is easy to use like classes (get and set properties)
    ///   - Is small to pass (just a pointer and an int, almost as small as passing an object)
    ///   - Is no-overhead (no per item object overhead)
    ///   - Is fast to serialize (serialize a column at a time)
    /// </summary>
    public struct SampleItem
    {
        // The item class only has a pointer to the set and the index of the item.
        private SampleSet _set;
        private int _index;

        // The constructor is internal only - the set must create the items
        internal SampleItem(SampleSet set, int index)
        {
            _set = set;
            _index = index;
        }

        // String properties are stored in a StringStore (and de-duped), which provides an int identifier
        // to look up strings. The friendly property here takes care of getting the identifier and then the
        // string.
        public String8 Name
        {
            get { return _set.Strings[_set.NameIdentifiers[_index]]; }
            set { _set.NameIdentifiers[_index] = _set.Strings.FindOrAddString(value); }
        }

        // String properties are of type "String8". String8 is a struct which wraps a UTF8 string within
        // a byte[]. This means strings are one byte per character (instead of two) and don't have per string
        // object overhead. [4 bytes for the position of the string in the byte[] and 4 byte for the identifier,
        // instead of 24b object overhead.
        public String8 Target
        {
            get { return _set.Strings[_set.TargetIdentifiers[_index]]; }
            set { _set.TargetIdentifiers[_index] = _set.Strings.FindOrAddString(value); }
        }

        // Enums aren't primitive types and can't be read and written as a block.
        // Enums are stored as the underlying type (byte, short, int) and converted by the friendly property.
        public SampleItemType Type
        {
            get { return (SampleItemType)_set.Types[_index]; }
            set { _set.Types[_index] = (byte)value; }
        }

        // DateTimes are also not primitives and are translated from them.
        public DateTime EventTime
        {
            get { return DateTimeExtensions.FromLong(_set.EventTimes[_index]); }
            set { _set.EventTimes[_index] = value.ToLong(); }
        }

        public override string ToString()
        {
            return _index.ToString();
        }
    }

    public class SampleSet : IReadOnlyList<SampleItem>, IBinarySerializable
    {
        // String Properties are stored in a StringStore.
        // Each Property has a PartialArray<int> holding the identifier (which string the item uses)
        internal StringStore Strings;
        internal PartialArray<int> NameIdentifiers;
        internal PartialArray<int> TargetIdentifiers;

        // Non-Primitive types (Enum, DateTime) are stored via Primitive equivalents and converted
        internal PartialArray<byte> Types;
        internal PartialArray<long> EventTimes;

        // The Set can have other structures to provide search and relationships
        // internal MemberIndex Index;
        // internal ItemMap Map;
        // internal ItemTree Tree;

        public SampleSet()
        {
            this.Clear();
        }

        public void Clear()
        {
            this.Strings = new StringStore();
            this.NameIdentifiers = new PartialArray<int>();
            this.TargetIdentifiers = new PartialArray<int>();
            this.Types = new PartialArray<byte>();
            this.EventTimes = new PartialArray<long>();
        }

        // Add adds a new SampleItem and returns it. If some values have to be
        // written on other data structures (indexing Name), those properties can
        // be arguments to add, or they can be indexed just before serialization.
        public SampleItem Add()
        {
            int index = this.NameIdentifiers.Count;
            this.NameIdentifiers.Add(0);
            this.TargetIdentifiers.Add(0);
            this.Types.Add((byte)SampleItemType.Basic);
            this.EventTimes.Add(default(DateTime).ToLong());

            return new SampleItem(this, index);
        }

        // The Set implements IReadOnlyList so that callers can get items.
        public SampleItem this[int index]
        {
            get { return new SampleItem(this, index); }
        }

        // The set exposes how many items it contains so they can be enumerated.
        public int Count
        {
            get { return this.NameIdentifiers.Count; }
        }

        IEnumerator<SampleItem> IEnumerable<SampleItem>.GetEnumerator()
        {
            return this.GetDefaultEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetDefaultEnumerator();
        }

        // The set exposes ConvertToImmutable to convert mutable (indexing time)
        // data structures to immutable (search time) versions. Items cannot be
        // added or changed after ConvertToImmutable is called. This design allows
        // the immutable forms to be optimized for search.
        public void ConvertToImmutable()
        {
            if (this.Strings.ConvertToImmutable())
            {
                this.Strings.ConvertToImmutable(this.NameIdentifiers);
                this.Strings.ConvertToImmutable(this.TargetIdentifiers);
            }
        }

        // The set implements WriteBinary to convert the arrays and write them quickly.
        public void WriteBinary(BinaryWriter w)
        {
            ConvertToImmutable();

            this.Strings.WriteBinary(w);
            this.NameIdentifiers.WriteBinary(w);
            this.TargetIdentifiers.WriteBinary(w);
            this.Types.WriteBinary(w);
            this.EventTimes.WriteBinary(w);
        }

        // The set exposes ReadBinary to read the arrays back in.
        public void ReadBinary(BinaryReader r)
        {
            this.Clear();

            this.Strings.ReadBinary(r);
            this.NameIdentifiers.ReadBinary(r);
            this.TargetIdentifiers.ReadBinary(r);
            this.Types.ReadBinary(r);
            this.EventTimes.ReadBinary(r);
        }
    }

    [TestClass]
    public class SampleStructureTests
    {
        [TestMethod]
        public void SampleStructure_Basic()
        {
            SampleSet set;
            SampleItem item;

            // Empty Set: Round trip and verify no errors
            set = new SampleSet();
            Assert.AreEqual(0, set.Count);
            set = Verify.RoundTrip(set);
            Assert.AreEqual(0, set.Count);

            // Rebuild and add items
            DateTime now = DateTime.UtcNow;

            set = new SampleSet();
            String8Block block = new String8Block();
            String8 name = block.GetCopy("Name");
            String8 target = block.GetCopy("Target");

            for (int i = 0; i < 100; ++i)
            {
                item = set.Add();
                item.Name = name;
                item.Target = target;
                item.Type = (i % 2 == 0 ? SampleItemType.Basic : SampleItemType.Complex);
                item.EventTime = now.AddMinutes(i);
            }

            Assert.AreEqual(100, set.Count);

            // Serialize and load
            set = Verify.RoundTrip(set);
            Assert.AreEqual(100, set.Count);

            // Verify items are correct
            for (int i = 0; i < set.Count; ++i)
            {
                item = set[i];
                Assert.AreEqual(name, item.Name);
                Assert.AreEqual(target, item.Target);
                Assert.AreEqual((i % 2 == 0 ? SampleItemType.Basic : SampleItemType.Complex), item.Type);
                Assert.AreEqual(now.AddMinutes(i), item.EventTime);
            }
        }
    }
}
