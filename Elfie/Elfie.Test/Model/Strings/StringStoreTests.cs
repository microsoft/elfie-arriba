// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model
{
    [TestClass]
    public class StringStoreTests
    {
        [TestMethod]
        public void MutableStringStore_Basic()
        {
            MutableStringStore store = new MutableStringStore();

            // Add strings
            int systemID = store.FindOrAddString("System");
            Assert.AreEqual(1, systemID, "Added strings get ascending integer IDs");

            int collectionsID = store.FindOrAddString("Collections");
            Assert.AreEqual(2, collectionsID, "Added strings get ascending integer IDs");

            store.FindOrAddString("Generic");
            store.FindOrAddString("List");

            Assert.AreEqual(0, store.FindOrAddString(null), "Null and Empty always are index 0.");
            Assert.AreEqual(0, store.FindOrAddString(string.Empty), "Null and Empty always are index 0.");

            // Add an already-added string
            int secondCollectionsID = store.FindOrAddString("Collections");
            Assert.AreEqual(collectionsID, secondCollectionsID);

            // Verify store is case sensitive [on add]
            int collectionsLowercaseID = store.FindOrAddString("collections");
            Assert.AreNotEqual(collectionsID, collectionsLowercaseID);

            // Try finding an existing string
            int foundCollectionsID;
            bool isCollectionsFound = store.TryFindString("Collections", out foundCollectionsID);
            Assert.IsTrue(isCollectionsFound);
            Assert.AreEqual(collectionsID, foundCollectionsID);

            // Try finding a missing string
            int notFoundID;
            bool isMissingThingFound = store.TryFindString("Regex", out notFoundID);
            Assert.IsFalse(isMissingThingFound);

            // Test round-tripping to an ImmutableStringStore
            IStringStore loadedStore = Convert(store);

            // Confirm "Collections" was first (after String.Empty sentinel)
            Assert.AreEqual("Collections", loadedStore[1].ToString());

            // Verify ImmutableStore finds both casings of Collections and returns the range
            Range matches;
            Assert.IsTrue(loadedStore.TryFindString("Collections", out matches));
            Assert.AreEqual("1-2", matches.ToString());
        }

        [TestMethod]
        public void ImmutableStringStore_Basic()
        {
            // Set of strings to index [out of order to verify GetSerializationIdentifier]
            string[] strings = { "Boolean", "Abacus", "Array", "ArrayList", "Editor", "Collections", "Dictionary" };
            int[] identifiers = new int[strings.Length];

            // Add values to a mutable store and track identifiers
            MutableStringStore store = new MutableStringStore();
            for (int i = 0; i < strings.Length; ++i)
            {
                identifiers[i] = store.FindOrAddString(strings[i]);
            }

            // Convert to immutable
            IStringStore iStore = Convert(store, identifiers);

            // Verify each value is found at the expected position
            byte[] buffer = new byte[32];
            for (int i = 0; i < strings.Length; ++i)
            {
                String8 value = String8.Convert(strings[i], buffer);

                Range foundAtIndex;
                Assert.IsTrue(iStore.TryFindString(value, out foundAtIndex), "ImmutableStore didn't contain added value \"{0}\"", strings[i]);
                Assert.AreEqual(identifiers[i], foundAtIndex.Start, "ImmutableStore didn't find value at SerializationIdentifier position");
                Assert.AreEqual(value, iStore[foundAtIndex.Start], "ImmutableStore didn't rebuild string with same value");
            }

            // Verify values not in collection aren't found and (single) insertion positions are returned
            Range matches;
            Assert.IsFalse(iStore.TryFindString("ZZ AfterLastValue", out matches));
            Assert.AreEqual((FindIdentifier("Editor", strings, identifiers) + 1).ToString(), matches.ToString());

            Assert.IsFalse(iStore.TryFindString("AA BeforeFirstValue", out matches));
            Assert.AreEqual(FindIdentifier("Abacus", strings, identifiers).ToString(), matches.ToString());

            Assert.IsFalse(iStore.TryFindString("Bz Between Boolean and Collections", out matches));
            Assert.AreEqual(FindIdentifier("Collections", strings, identifiers).ToString(), matches.ToString());

            Assert.IsFalse(iStore.TryFindString("Cz Between Collections and Dictionary", out matches));
            Assert.AreEqual(FindIdentifier("Dictionary", strings, identifiers).ToString(), matches.ToString());

            // Check range searches
            Assert.AreEqual("Array-ArrayList", GetRangeValuesAsString("Arr", iStore), "Prefix covering multiple items should match all");
            Assert.AreEqual("Array-ArrayList", GetRangeValuesAsString("Array", iStore), "Prefix equalling an item should include it");
            Assert.AreEqual("ArrayList", GetRangeValuesAsString("ArrayL", iStore), "Prefix of only one item has only it");
            Assert.AreEqual(string.Empty, GetRangeValuesAsString("ArrayList2", iStore), "Prefix longer than item should not match");
            Assert.AreEqual("Abacus-ArrayList", GetRangeValuesAsString("A", iStore), "Prefix containing first item should include it");
            Assert.AreEqual("Editor", GetRangeValuesAsString("Edit", iStore), "Prefix containing last item should include it");
            Assert.AreEqual(string.Empty, GetRangeValuesAsString("AA", iStore), "Prefix before first item has empty range");
            Assert.AreEqual(string.Empty, GetRangeValuesAsString("ZZ", iStore), "Prefix after last item has empty range");
        }

        [TestMethod]
        public void StringStore_CaseSensitivity()
        {
            // Sample Strings: Not all in order, including casing differences, including duplicates
            StringStore store = new StringStore();
            string[] strings = { "bool", "bool", "boolean", "Boolean", "BOOLEAN", "array", "Array", "aRRay", "ARRAY", "Array", "Collections", "ARR", "BIT" };
            int[] addedIDs = new int[strings.Length];

            // Add each value
            for (int i = 0; i < strings.Length; ++i)
            {
                addedIDs[i] = store.FindOrAddString(strings[i]);
            }

            // Verify each value comes back cased correctly (case sensitive add)
            for (int i = 0; i < strings.Length; ++i)
            {
                Assert.AreEqual(strings[i], store[addedIDs[i]].ToString());
            }

            // Convert to Immutable
            store.ConvertToImmutable();

            // Remap IDs
            for (int i = 0; i < strings.Length; ++i)
            {
                addedIDs[i] = store.GetSerializationIdentifier(addedIDs[i]);
            }

            // Verify each value comes back cased correctly (case sensitive values preserved on convert)
            for (int i = 0; i < strings.Length; ++i)
            {
                Assert.AreEqual(strings[i], store[addedIDs[i]].ToString());
            }

            // Verify values have ascending IDs and are in case insensitive *stable* order
            string last = store[0].ToString();
            for (int i = 1; i < store.Count; ++i)
            {
                string current = store[i].ToString();

                // Verify all strings are in case insensitive order
                int cmp = string.Compare(last, current, StringComparison.OrdinalIgnoreCase);
                Assert.IsTrue(cmp <= 0);

                // Verify case-insensitive ties are in case sensitive order relative to each other
                if (cmp == 0)
                {
                    Assert.IsTrue(string.Compare(last, current, StringComparison.Ordinal) < 0);
                }
                last = current;
            }

            // Verify searches return the range of capitalizations for the value
            byte[] buffer = new byte[20];
            for (int i = 0; i < strings.Length; ++i)
            {
                String8 value8 = String8.Convert(strings[i], buffer);

                // Verify the string is found
                Range range;
                Assert.IsTrue(store.TryFindString(value8, out range));

                // Verify the ID for the exact casing is reported within the range
                Assert.IsTrue(range.Contains(addedIDs[i]));

                // Verify every value in the range matches the value (case-insensitive)
                for (int j = range.Start; j <= range.End; ++j)
                {
                    String8 otherMatch = store[j];
                    Assert.AreEqual(0, value8.CompareTo(otherMatch, true), String.Format("'{0}' in match range wasn't reported equal to '{1}' being matched", otherMatch, value8));
                }

                // Verify the values just before and after the range don't match
                if (range.Start > 0)
                {
                    String8 valueBefore = store[range.Start - 1];
                    Assert.IsTrue(value8.CompareTo(valueBefore, true) > 0, String.Format("'{0}' before match range wasn't reported before '{1}' being matched", valueBefore, value8));
                }

                if (range.End < store.Count - 1)
                {
                    String8 valueAfter = store[range.End + 1];
                    Assert.IsTrue(value8.CompareTo(valueAfter, true) < 0, String.Format("'{0}' after match range wasn't reported after '{1}' being matched", valueAfter, value8));
                }

                // Ask for the case-sensitive range
                Range caseSensitive;
                Assert.IsTrue(store.TryFindString(value8, false, out caseSensitive));

                // Verify every value in the range matches the value (case-sensitive)
                for (int j = caseSensitive.Start; j <= caseSensitive.End; ++j)
                {
                    String8 otherMatch = store[j];
                    Assert.AreEqual(0, value8.CompareTo(otherMatch, false), String.Format("'{0}' in case sensitive range wasn't reported equal to '{1}' being matched", otherMatch, value8));
                }

                // Verify the values just before and after the range don't match
                if (caseSensitive.Start > 0)
                {
                    String8 valueBefore = store[caseSensitive.Start - 1];
                    Assert.IsTrue(value8.CompareTo(valueBefore, false) != 0, String.Format("'{0}' before case sensitive range still matches '{1}'", valueBefore, value8));
                }

                if (caseSensitive.End < store.Count - 1)
                {
                    String8 valueAfter = store[caseSensitive.End + 1];
                    Assert.IsTrue(value8.CompareTo(valueAfter, false) != 0, String.Format("'{0}' after case sensitive range still matches '{1}'", valueAfter, value8));
                }
            }

            // Verify MakeCaseSensitive goes to empty if the provided casing isn't any of the values
            String8 BOOLean = String8.Convert("BOOLean", buffer);
            Range booleanRange;
            Assert.IsFalse(store.TryFindString(BOOLean, false, out booleanRange));
        }

        //#if PERFORMANCE
        //        [TestMethod]
        //#endif
        //        public void StringStore_Performance()
        //        {
        //            SelfIndex.EnsureSampleIndexBuilt();
        //            ImmutableStringStore store = new ImmutableStringStore();
        //            store.FileRead(SelfIndex.StringStorePath);

        //            // Choose some sample values to find
        //            string[] valuesToFind =
        //            {
        //                        "BinaryReader",
        //                        "ConvertToImmutable",
        //                        "FileIO.cs",
        //                        "IBinarySerializable, string, bool",
        //                        "IWriteableString",
        //                        "string, string, bool, bool",
        //                        "WasMemberAdded",
        //                        "WriteTree",
        //                        "Action<Symbol>",
        //                        "IMemberDatabase, PartialArray<Symbol>"
        //                    };
        //            String8[] value8sToFind = new String8[valuesToFind.Length];
        //            Range[] identifiers = new Range[valuesToFind.Length];

        //            // Get identifiers and the values and confirm they work
        //            for (int i = 0; i < identifiers.Length; ++i)
        //            {
        //                Assert.IsTrue(store.TryFindString(valuesToFind[i], out identifiers[i]), string.Format("Could not find \"{0}\"", valuesToFind[i]));
        //                Assert.AreEqual(valuesToFind[i], store[identifiers[i].Start].ToString());
        //                value8sToFind[i] = String8.Convert(valuesToFind[i], new byte[String8.GetLength(valuesToFind[i])]);
        //            }

        //            // Measure these operations in a tight loop [~2M/sec with case insensitive range for string]
        //            // TryFindString Goal: 2M/sec
        //            Verify.PerformanceByOperation(2 * LongExtensions.Million, () =>
        //            {
        //                int iterations = 10000;
        //                for (int iteration = 0; iteration < iterations; ++iteration)
        //                {
        //                    for (int i = 0; i < identifiers.Length; ++i)
        //                    {
        //                        store.TryFindString(value8sToFind[i], out identifiers[i]);
        //                    }
        //                }

        //                return iterations * identifiers.Length;
        //            });

        //            // GetString Goal: 20M/sec
        //            Verify.PerformanceByOperation(20 * LongExtensions.Million, () =>
        //            {
        //                int iterations = 100000;
        //                for (int iteration = 0; iteration < iterations; ++iteration)
        //                {
        //                    for (int i = 0; i < identifiers.Length; ++i)
        //                    {
        //                        String8 value = store[identifiers[i].Start];
        //                    }
        //                }

        //                return iterations * identifiers.Length;
        //            });
        //        }

        private static IStringStore Convert(MutableStringStore store, int[] identifiers = null)
        {
            int[] identifierMap;
            IStringStore convertedStore = store.ConvertToImmutable(out identifierMap);

            // Convert identifiers when added to 'values to serialize'
            if (identifiers != null)
            {
                for (int i = 0; i < identifiers.Length; ++i)
                {
                    identifiers[i] = identifierMap[identifiers[i]];
                }
            }

            // Write the Converted Store and read as an ImmutableStore
            ImmutableStringStore roundTrippedStore = new ImmutableStringStore();

            MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                convertedStore.WriteBinary(writer);

                stream.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(stream);
                roundTrippedStore.ReadBinary(reader);
            }

            return roundTrippedStore;
        }

        private int FindIdentifier(string value, string[] values, int[] identifiers)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                if (value.Equals(values[i])) return identifiers[i];
            }

            throw new KeyNotFoundException();
        }

        private static string GetRangeValuesAsString(string prefix, IStringStore store)
        {
            Range matches;
            if (!store.TryGetRangeStartingWith(String8.Convert(prefix, new byte[String8.GetLength(prefix)]), out matches)) return string.Empty;

            if (matches.Start == matches.End)
            {
                return store[matches.Start].ToString();
            }
            else
            {
                return string.Format("{0}-{1}", store[matches.Start], store[matches.End]);
            }
        }
    }
}
