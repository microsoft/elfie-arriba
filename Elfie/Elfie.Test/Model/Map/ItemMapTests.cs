// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Model.Map
{
    [TestClass]
    public class ItemMapTests
    {
        [TestMethod]
        public void ItemMap_Basics()
        {
            SampleSet set = new SampleSet();

            // Build an empty map
            ItemMap<SampleItem> map = new ItemMap<SampleItem>(set);

            // Verify search before save doesn't work
            Verify.Exception<InvalidOperationException>(() => map.LinksFrom(0));

            // Verify Save and Reload has no errors
            map = Verify.RoundTrip(map);

            // Verify add after load doesn't work
            Verify.Exception<InvalidOperationException>(() => map.AddLink(1, 2));

            // Verify search returns a valid but empty set from an empty map
            Assert.AreEqual(0, map.LinksFrom(0).Count);


            // Rebuild map
            map = new ItemMap<SampleItem>(set);

            // Add links (in order and out of order)
            map.AddLink(0, 1);
            map.AddLink(0, 2);
            map.AddLink(1, 2);
            map.AddLink(2, 3);
            map.AddLink(4, 0);

            // Add duplicate links
            map.AddLink(1, 2);
            map.AddLink(1, 2);

            // Save, then add links after conversion (verify error)
            map = Verify.RoundTrip(map);

            // Ask for links (valid item with no links)
            Assert.AreEqual(0, map.LinksFrom(3).Count);

            // Ask for links (item with one link)
            Assert.AreEqual("3", StringExtensions.Join(", ", map.LinksFrom(2)));

            // Ask for links (item with many links)
            Assert.AreEqual("1, 2", StringExtensions.Join(", ", map.LinksFrom(0)));

            // Ask for links (verify no duplicates)
            Assert.AreEqual("2", StringExtensions.Join(", ", map.LinksFrom(1)));

            // Ask for links (first and last items)
            Assert.AreEqual("1, 2", StringExtensions.Join(", ", map.LinksFrom(0)));
            Assert.AreEqual("0", StringExtensions.Join(", ", map.LinksFrom(4)));

            // Ask for links (unknown item) (verify error)
            Assert.AreEqual("", StringExtensions.Join(", ", map.LinksFrom(5)));

            // MapEnumerator.Reset works (and Dispose works)
            using (MapEnumerator<SampleItem> links = map.LinksFrom(0))
            {
                while (links.MoveNext()) ;
                Assert.AreEqual("", StringExtensions.Join(", ", links));
                links.Reset();
                Assert.AreEqual("1, 2", StringExtensions.Join(", ", links));
            }

            // MapEnumerator.GetCurrent untyped works.
            IEnumerator untypedLinks = map.LinksFrom(0);
            untypedLinks.MoveNext();
            object first = untypedLinks.Current;
            Assert.AreEqual("1", first.ToString());
        }
    }
}
