// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model
{
    [TestClass]
    public class ItemTreeTests
    {
        [TestMethod]
        public void ItemTree_Basic()
        {
            byte[] byteBuffer = new byte[100];
            int[] intBuffer = new int[10];
            String8Set splitPath8;

            StringStore strings = new StringStore();
            ItemTree fileTree = new ItemTree();

            string[] filePaths = {
                @"C:\Code\Arriba\Arriba\Diagnostics\DailyLogTraceListener.cs",
                @"C:\Code\Arriba\Arriba\Diagnostics\ProgressWriter.cs",
                @"C:\Code\Arriba\Arriba\Diagnostics\Log4NetDiagnosticConsumer.cs",
                @"C:\Code\Arriba\Arriba\Diagnostics\Memory.cs",
                @"C:\Code\Arriba\Arriba\Diagnostics\TraceWriter.cs"
            };

            List<int> fileTreeIndexes = new List<int>();

            // Index each file path
            foreach (string filePath in filePaths)
            {
                splitPath8 = String8.Convert(filePath, byteBuffer).Split('\\', intBuffer);
                fileTreeIndexes.Add(fileTree.AddPath(0, splitPath8, strings));
            }

            for (int i = 0; i < filePaths.Length; ++i)
            {
                // Reconstruct each file path and confirm they match
                string rebuiltPath = fileTree.GetPath(fileTreeIndexes[i], strings, '\\').ToString();
                Assert.AreEqual(filePaths[i], rebuiltPath);

                // Verify find by path works
                splitPath8 = String8.Convert(filePaths[i], byteBuffer).Split('\\', intBuffer);
                int foundAtIndex;
                Assert.IsTrue(fileTree.TryFindByPath(0, splitPath8, strings, out foundAtIndex));
                Assert.AreEqual(fileTreeIndexes[i], foundAtIndex);
            }

            // Verify find works
            int foundIndex;

            // Root found under sentinel root
            Assert.IsTrue(fileTree.TryFindChildByName(0, strings.FindOrAddString("C:"), out foundIndex));
            Assert.AreEqual(1, foundIndex);

            // Root not found under another node
            Assert.IsFalse(fileTree.TryFindChildByName(1, strings.FindOrAddString("C:"), out foundIndex));

            // Node not found at root
            Assert.IsFalse(fileTree.TryFindChildByName(0, strings.FindOrAddString("Code"), out foundIndex));

            // Node found under the right parent
            Assert.IsTrue(fileTree.TryFindChildByName(1, strings.FindOrAddString("Code"), out foundIndex));
            Assert.AreEqual(2, foundIndex);


            // FindByPath works under a partial path
            splitPath8 = String8.Convert(@"Code\Arriba", byteBuffer).Split('\\', intBuffer);
            int arribaIndex;
            Assert.IsTrue(fileTree.TryFindByPath(1, splitPath8, strings, out arribaIndex));

            splitPath8 = String8.Convert(@"Arriba\Diagnostics\DailyLogTraceListener.cs", byteBuffer).Split('\\', intBuffer);
            int dailyLogIndex;
            Assert.IsTrue(fileTree.TryFindByPath(arribaIndex, splitPath8, strings, out dailyLogIndex));
            Assert.AreEqual(fileTreeIndexes[0], dailyLogIndex);


            // FindByPath returns the closest element when it fails
            splitPath8 = String8.Convert(@"C:\Nope", byteBuffer).Split('\\', intBuffer);
            int nopeIndex;
            Assert.IsFalse(fileTree.TryFindByPath(0, splitPath8, strings, out nopeIndex));
            Assert.AreEqual(1, nopeIndex, @"Failed find for C:\Nope should return 'C:' index; the successful portion of the search.");

            splitPath8 = String8.Convert(@"C:\Code\Arriba\Arriba\Diagnostics\TraceWriter.cs\Nope", byteBuffer).Split('\\', intBuffer);
            Assert.IsFalse(fileTree.TryFindByPath(0, splitPath8, strings, out nopeIndex));
            Assert.AreEqual(fileTreeIndexes[4], nopeIndex);


            // Verify depth works
            Assert.AreEqual(0, fileTree.GetDepth(0));
            Assert.AreEqual(1, fileTree.GetDepth(1));
            Assert.AreEqual(6, fileTree.GetDepth(fileTreeIndexes[0]));
            Assert.AreEqual("C:", strings[fileTree.GetNameIdentifier(fileTree.GetAncestorAtDepth(fileTreeIndexes[0], 1))].ToString());
            Assert.AreEqual("Code", strings[fileTree.GetNameIdentifier(fileTree.GetAncestorAtDepth(fileTreeIndexes[0], 2))].ToString());
            Assert.AreEqual("Arriba", strings[fileTree.GetNameIdentifier(fileTree.GetAncestorAtDepth(fileTreeIndexes[0], 3))].ToString());
            Assert.AreEqual("Arriba", strings[fileTree.GetNameIdentifier(fileTree.GetAncestorAtDepth(fileTreeIndexes[0], 4))].ToString());

            // Sort the tree by name
            fileTree.SortByName(strings);

            // Log the tree
            Trace.WriteLine(Write.ToString((w) => fileTree.WriteTree(w, strings, 1)));

            // Verify roundtrip
            ItemTree readTree = new ItemTree();
            Verify.RoundTrip(fileTree, readTree);
            fileTree = readTree;

            // Reconstruct each file path
            for (int i = 0; i < filePaths.Length; ++i)
            {
                string rebuiltPath = fileTree.GetPath(fileTreeIndexes[i], strings, '\\').ToString();
                Assert.AreEqual(filePaths[i], rebuiltPath);
            }
        }

        public static int FindByPath(ItemTree tree, StringStore strings, string path, char delimiter = '\\')
        {
            String8 path8 = String8.Convert(path, new byte[String8.GetLength(path)]);
            String8Set pathSplit8 = path8.Split(delimiter, new int[String8Set.GetLength(path8, delimiter)]);
            return tree.FindByPath(0, pathSplit8, strings);
        }
    }
}
