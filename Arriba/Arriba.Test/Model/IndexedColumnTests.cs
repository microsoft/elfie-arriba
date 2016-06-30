// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Indexing;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class IndexedColumnTests
    {
        [TestMethod]
        public void IndexedColumn_Basic()
        {
            ColumnTests.ColumnTest_Basics(() => new IndexedColumn(ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0), new DefaultWordSplitter()), "One", "Two");
        }

        [TestMethod]
        public void IndexedColumn_Where()
        {
            IColumn<ByteBlock> c = new IndexedColumn(ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0), new DefaultWordSplitter());

            // Verify no error searching empty column
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Matches, "missing"));

            c.SetSize(4);
            c[0] = "First Value";
            c[1] = "Second Value";
            c[2] = "this is a different value";
            ColumnTests.AssertConsistent(c);

            // Verify passthrough to base column
            Assert.AreEqual("0", ColumnTests.GetMatches(c, Operator.Equals, "First Value"));

            // Verify unknown words don't match (no error)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Matches, "missing"));

            // Verify all match for shared words
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.Matches, "value"));
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.MatchesExact, "value"));

            // Verify prefixes match for 'Matches' and not 'MatchesExact'
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.Matches, "val"));
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.MatchesExact, "val"));

            // Verify unique words return only those matches
            Assert.AreEqual("0", ColumnTests.GetMatches(c, Operator.Matches, "first"));
            Assert.AreEqual("2", ColumnTests.GetMatches(c, Operator.Matches, "diff"));

            // Verify matches fallthrough to equals
            Assert.AreEqual("0", ColumnTests.GetMatches(c, Operator.Matches, "First Value"));

            c[0] = "Updated Value Here";
            ColumnTests.AssertConsistent(c);

            // Verify items no longer match for removed words
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Matches, "first"));

            // Verify items continue to match for kept words
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.MatchesExact, "value"));

            // Verify items match for new words
            Assert.AreEqual("0", ColumnTests.GetMatches(c, Operator.MatchesExact, "updated"));
        }

        [TestMethod]
        public void IndexedColumn_DictionaryConversion()
        {
            IndexedColumn c = new IndexedColumn(ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0), new DefaultWordSplitter());

            c.SetSize(4);
            c[0] = "First Value";
            c[1] = "Second Value";
            c[2] = "this is a different value";
            ColumnTests.AssertConsistent(c);

            // Verify Dictionary conversion
            Dictionary<string, List<ushort>> dictionary = c.ConvertToDictionary();
            Assert.AreEqual(7, dictionary.Keys.Count);
            Assert.AreEqual("0, 1, 2", String.Join(", ", dictionary["value"]));
            Assert.AreEqual("0", String.Join(", ", dictionary["first"]));
            Assert.AreEqual("1", String.Join(", ", dictionary["second"]));
            Assert.AreEqual("2", String.Join(", ", dictionary["this"]));
            Assert.AreEqual("2", String.Join(", ", dictionary["is"]));
            Assert.AreEqual("2", String.Join(", ", dictionary["a"]));
            Assert.AreEqual("2", String.Join(", ", dictionary["different"]));
        }

        [TestMethod]
        public void IndexedColumn_HtmlSplitter()
        {
            IColumn<object> c = ColumnFactory.Build(new ColumnDetails("Description", "indexed[html]:sorted:string", null), 0);

            c.SetSize(4);
            c[0] = "<span title='nice'>First Value</span>";
            c[1] = "<div>Second <b>Value</b> &nbsp;</div>";
            c[2] = "this is a <b>different</b>&nbsp;value";
            ColumnTests.AssertConsistent(c);

            // Verify words in html don't match (Splitter Should exclude)
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Matches, "span"));
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Matches, "nice"));
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.Matches, "nbsp"));

            // Verify text within html does match
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.Matches, "value"));
            Assert.AreEqual("0", ColumnTests.GetMatches(c, Operator.Matches, "first"));
            Assert.AreEqual("2", ColumnTests.GetMatches(c, Operator.Matches, "different"));
        }

        [TestMethod]
        public void IndexedColumn_Churn()
        {
            IColumn<ByteBlock> c = new IndexedColumn(ColumnFactory.CreateSortedColumn<ByteBlock>(new ByteBlockColumn(ByteBlock.Zero), 0), new DefaultWordSplitter());
            c.SetSize(4);
            c[0] = "First Value";
            c[1] = "Second Value";
            c[2] = "this value is yet another one";
            ColumnTests.AssertConsistent(c);

            // Remove the last word added and ensure no problems (unique case, no swap and remove)
            Assert.AreEqual("2", ColumnTests.GetMatches(c, Operator.MatchesExact, "one"));
            c[2] = "this value is yet another";
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.MatchesExact, "one"));
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.MatchesExact, "value"));

            // Reset a value to itself (reusing the read ByteBlock); ensure no problems
            c[1] = c[1];
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("0, 1, 2", ColumnTests.GetMatches(c, Operator.MatchesExact, "value"));

            // Set a value to null; ensure no problems
            c[0] = (ByteBlock)(string)null;
            ColumnTests.AssertConsistent(c);
            Assert.AreEqual("", ColumnTests.GetMatches(c, Operator.MatchesExact, "first"));
            Assert.AreEqual("1, 2", ColumnTests.GetMatches(c, Operator.MatchesExact, "value"));
        }
    }
}
