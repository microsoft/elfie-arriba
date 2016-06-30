// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arriba.Indexing;
using Arriba.Model;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class HighlighterTests
    {
        [TestMethod]
        public void Highlighter_Highlight_Basic()
        {
            Highlighter h = new Highlighter("[", "]");

            IWordSplitter splitter = new HtmlWordSplitter(new DefaultWordSplitter());

            List<Highlighter.HighlightTerm> terms = new List<Highlighter.HighlightTerm>();
            terms.Add(new Highlighter.HighlightTerm("active", false));
            terms.Add(new Highlighter.HighlightTerm("edi", false));
            terms.Add(new Highlighter.HighlightTerm("39400", true));

            // Verify:
            //  - Terms are highlighted
            //  - ExactTerms don't highlight words they are prefixes of
            //  - Casing differences are ignored; casing in source value preserved
            //  - Values in Html are not highlighted
            //  - Prefix/Suffix around highlights also included
            Assert.AreEqual("[active] <div class='active'>Sample Value</div> [Edi]tors [39400] found within 394000 today", h.Highlight("active <div class='active'>Sample Value</div> Editors 39400 found within 394000 today", splitter, terms).ToString());

            // Verify: Same ByteBlock returned when no matches
            ByteBlock noMatches = "no matches here";
            Assert.AreSame(noMatches.Array, h.Highlight(noMatches, splitter, terms).Array);

            // Verify:
            //  - Terms at very start and end work ok
            Assert.AreEqual("[edi]tors [39400]", h.Highlight("editors 39400", splitter, terms).ToString());
        }

        [TestMethod]
        public void ByteBlockAppender_Basic()
        {
            Highlighter.ByteBlockAppender appender;
            ByteBlock rawValue = ByteBlock.TestBlock("active <div class='active'>Sample Value</div> Editors 39400 found within 394000 today");

            // If no changes, verify same array returned
            appender = new Highlighter.ByteBlockAppender(rawValue);
            appender.AppendRemainder();
            Assert.AreSame(rawValue.Array, appender.Value().Array);

            // Verify AppendTo current position does nothing
            appender = new Highlighter.ByteBlockAppender(rawValue);
            appender.AppendTo(0);
            Assert.AreEqual("", appender.Value().ToString());

            // Verify Append, AppendTo properly wrap value
            appender.Append("[");
            Assert.IsTrue(appender.AppendTo(6));
            appender.Append("]");
            Assert.AreEqual("[active]", appender.Value().ToString());

            // Verify AppendTo properly tracks yet-to-add content
            appender.AppendTo(46);
            appender.Append("[");
            appender.AppendTo(49);
            appender.Append("]");
            Assert.AreEqual("[active] <div class='active'>Sample Value</div> [Edi]", appender.Value().ToString());

            // Verify AppendTo will not re-highlight previous content (for words split multiple ways)
            Assert.IsFalse(appender.AppendTo(46));

            // Verify AppendTo handles back-to-back wrapping
            Assert.IsTrue(appender.AppendTo(49));
            appender.Append("[");
            appender.AppendTo(53);
            appender.Append("]");
            Assert.AreEqual("[active] <div class='active'>Sample Value</div> [Edi][tors]", appender.Value().ToString());

            // Verify AppendRemainder catches rest
            appender.AppendRemainder();
            Assert.AreEqual("[active] <div class='active'>Sample Value</div> [Edi][tors] 39400 found within 394000 today", appender.Value().ToString());
        }
    }
}
