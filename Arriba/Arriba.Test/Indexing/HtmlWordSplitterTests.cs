// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Indexing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Indexing
{
    [TestClass]
    public class HtmlWordSplitterTests : WordSplitterTestBase
    {
        public HtmlWordSplitterTests() :
            base(new HtmlWordSplitter(new DefaultWordSplitter()))
        { }

        [TestMethod]
        public void HtmlWordSplitter_Basic()
        {
            Assert.AreEqual(String.Empty, SplitAndJoin(String.Empty));
            Assert.AreEqual("literal|text|only", SplitAndJoin("literal text only"));
            Assert.AreEqual("Prefix|Div|Content|Suffix", SplitAndJoin("Prefix<div>Div Content</div>Suffix"));
            Assert.AreEqual("This|That", SplitAndJoin("<div class='sample' title=\"&quot;Five&quot;\">This &amp; That</div>"));
            Assert.AreEqual("Content|with|multiple|div|s|to|figure|out", SplitAndJoin("Content with <b>multiple</b> &lt;div&gt;s to figure out"));
        }

        [TestMethod]
        public void HtmlWordSplitter_InlineImage()
        {
            Assert.AreEqual("Before|After", SplitAndJoin("Before<IMG title=\"Click\" src=\"data:image/Png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAABk+/f3gm7==\">After"));
        }
    }
}
