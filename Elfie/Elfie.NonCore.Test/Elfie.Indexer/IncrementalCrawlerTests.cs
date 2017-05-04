// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Elfie.Indexer
{
    [TestClass]
    public class IncrementalCrawlerTests
    {
        [TestMethod]
        public void IncrementalCrawler_Basic()
        {
            RoslynCompilationCrawler rcc = new RoslynCompilationCrawler();
            rcc.IncludeCodeLocations = false;
            rcc.IncludeMembers = false;

            CrawlCounter counter = new CrawlCounter(rcc);

            // Walk. Verify at least three binaries were found (Elfie, Elfie.Indexer, Elfie.Test)
            IncrementalCrawler ic = new IncrementalCrawler(counter);
            PackageDatabase db = ic.Walk(".");
            Assert.IsTrue(counter.Count >= 3);

            db.ConvertToImmutable();

            DateTime utcNow = DateTime.UtcNow;

            // Walk again. Verify nothing is new.
            counter.Count = 0;
            ic = new IncrementalCrawler(counter, db, utcNow);
            PackageDatabase rebuild = ic.Walk(".");
            Assert.AreEqual(0, counter.Count);
            Assert.AreEqual(db.MemberCount, rebuild.MemberCount);
        }
    }

    internal class CrawlCounter : ICrawler
    {
        public int Count { get; set; }
        private ICrawler InnerCrawler { get; set; }

        public CrawlCounter(ICrawler innerCrawler)
        {
            this.InnerCrawler = innerCrawler;
        }

        public void Walk(string filePath, MutableSymbol parent)
        {
            this.Count++;
            this.InnerCrawler.Walk(filePath, parent);
        }
    }
}
