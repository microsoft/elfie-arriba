// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test
{
    public enum SampleItemType : byte
    {
        Unknown = 0,
        Basic = 1,
        Complex = 2
    }

    // Sample Data structure the normal .NET class way
    public class SampleItemClass
    {
        public string Name;
        public string Target;
        public SampleItemType Type;
        public DateTime EventTime;
    }

    // Set of SampleItemClass the normal .NET class way
    public class SampleItemClassSet
    {
        public SampleItemClass[] Set;
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

        // String properties are stored as "String8" (a String in UTF8 byte[])
        public String8 Name
        {
            get { return _set.Name[_index]; }
            set { _set.Name[_index] = value; }
        }

        public String8 Target
        {
            get { return _set.Target[_index]; }
            set { _set.Target[_index] = value; }
        }

        // Enums and other non-primitives must be stored as primitives and converted
        public SampleItemType Type
        {
            get { return (SampleItemType)_set.Type[_index]; }
            set { _set.Type[_index] = (byte)value; }
        }

        public DateTime EventTime
        {
            get { return _set.EventTime[_index].ToDateTime(); }
            set { _set.EventTime[_index] = value.ToLong(); }
        }

        public override string ToString()
        {
            return _index.ToString();
        }
    }

    // The Set class inherits from BaseItemSet, which provides the StringStore and overall management
    public class SampleSet : BaseItemSet<SampleItem>
    {
        // String values are stored in 'String8Column', which manages lookup
        internal String8Column Name;
        internal String8Column Target;

        // Other values are stored in PartialArrays of primitive types and converted by properties
        internal PartialArray<byte> Type;
        internal PartialArray<long> EventTime;

        // Each column needs to be constructed and added to the BaseItemSet collection
        public SampleSet() : base()
        {
            this.Name = new String8Column(this.Strings);
            this.AddColumn(this.Name);

            this.Target = new String8Column(this.Strings);
            this.AddColumn(this.Target);

            this.Type = new PartialArray<byte>();
            this.AddColumn(this.Type);

            this.EventTime = new PartialArray<long>();
            this.AddColumn(this.EventTime);
        }

        // The set must implement an indexer to build the corresponding item. BaseItemSet implements the rest of IReadOnlyList<T>
        public override SampleItem this[int index]
        {
            get { return new SampleItem(this, index); }
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
            SampleSet readSet = new SampleSet();
            Verify.RoundTrip(set, readSet);
            Assert.AreEqual(0, readSet.Count);

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
            readSet = new SampleSet();
            Verify.RoundTrip(set, readSet);
            Assert.AreEqual(100, readSet.Count);

            // Verify items are correct
            for (int i = 0; i < readSet.Count; ++i)
            {
                item = readSet[i];
                Assert.AreEqual(name, item.Name);
                Assert.AreEqual(target, item.Target);
                Assert.AreEqual((i % 2 == 0 ? SampleItemType.Basic : SampleItemType.Complex), item.Type);
                Assert.AreEqual(now.AddMinutes(i), item.EventTime);
            }
        }
    }
}
