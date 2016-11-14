// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Indexing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Indexing
{
    [TestClass]
    public class SetSplitterTests : WordSplitterTestBase
    {
        public SetSplitterTests() :
            base(new SetSplitter())
        { }

        [TestMethod]
        public void SetSplitter_Basic()
        {
            Assert.AreEqual("", SplitAndJoin(""));
            Assert.AreEqual("Single Value, no semi-colon", SplitAndJoin("Single Value, no semi-colon"));
            Assert.AreEqual("One|Two", SplitAndJoin("One; Two"));
            Assert.AreEqual("No|Space|Between|Values", SplitAndJoin("No;Space;Between;Values"));
            Assert.AreEqual("Leading|And|Trailing", SplitAndJoin(";Leading;And;Trailing;"));
        }
    }
}
