// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Arriba.Diagnostics;
using Arriba.Extensions;
using Arriba.Structures;
using Arriba.TfsWorkItemCrawler.ItemConsumers;
using Arriba.TfsWorkItemCrawler.ItemProviders;

namespace Arriba.TfsWorkItemCrawler
{
    public class DefaultCrawler
    {
        // Use a large batch size to avoid many small writes followed by frequent Saves
        private const int BatchSize = 100;

        // Save periodically during huge crawls to avoid losing everything if the service goes down
        private const int WriteAfterMinutes = 60;

        // Maximum number of parallel item set requests we'll simultaneously issue
        private const int CrawlMaxParallelism = 8;

        private string ConfigurationName { get; set; }
        private CrawlerConfiguration Configuration { get; set; }
        private bool Rebuild { get; set; }

        private IEnumerable<string> ColumnNames { get; set; }

        public DefaultCrawler(CrawlerConfiguration config, IEnumerable<string> columnNames, string configurationName, bool rebuild)
        {
            this.ConfigurationName = configurationName;
            this.Configuration = config;
            this.Rebuild = rebuild;

            this.ColumnNames = columnNames;
        }

        public void Crawl(IItemProvider provider, IItemConsumer consumer)
        {
            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = CrawlMaxParallelism };
            object locker = new object();

            int exceptionCount = 0;
            int itemCount = 0;
            DateTime lastChangedItemAppended = DateTime.MinValue;
            Stopwatch readWatch = new Stopwatch();
            Stopwatch writeWatch = new Stopwatch();
            Stopwatch saveWatch = new Stopwatch();
            Stopwatch sinceLastWrite = null;

            try
            {
                DateTime previousLastChangedItem = ItemProviderUtilities.LoadLastCutoff(this.Configuration.ArribaTable, this.ConfigurationName, this.Rebuild);
                DateTime now = DateTime.UtcNow;
                lastChangedItemAppended = previousLastChangedItem;

                Trace.WriteLine(string.Format("Last Updated item was updated at '{0}'...", previousLastChangedItem));

                // For clean crawl, get more than a day at a time until first items found
                int intervalDays = ((now - previousLastChangedItem).TotalDays > 365 ? 365 : 1);

                DateTime end;
                for (DateTime start = previousLastChangedItem; start <= now; start = end)
                {
                    end = start.AddDays(intervalDays);

                    // Find the set of items to retrieve
                    Trace.WriteLine(string.Format("Identifying items changed between '{0}' and '{1}'...", start, end));
                    IList<ItemIdentity> itemsToGet = null;
                    itemsToGet = provider.GetItemsChangedBetween(start, end);

                    // If few or no items are returned, crawl by week. If many, by day
                    if (itemsToGet != null && itemsToGet.Count > 1000)
                    {
                        intervalDays = 1;
                    }
                    else
                    {
                        if (intervalDays != 365) intervalDays = 7;
                    }

                    // If no items in this batch, get the next batch
                    if (itemsToGet == null || itemsToGet.Count == 0) continue;

                    // After getting the first item list, save every 30 minutes
                    if (sinceLastWrite == null) sinceLastWrite = Stopwatch.StartNew();

                    // Get the items in blocks in ascending order by Changed Date [restartability]
                    Trace.WriteLine(string.Format("Downloading {0:n0} items...", itemsToGet.Count));

                    List<IList<ItemIdentity>> pages = new List<IList<ItemIdentity>>(itemsToGet.OrderBy(ii => ii.ChangedDate).Page(BatchSize));

                    for (int nextPageIndex = 0; nextPageIndex < pages.Count; nextPageIndex += CrawlMaxParallelism)
                    {
                        try
                        {
                            int pageCountThisIteration = Math.Min(CrawlMaxParallelism, pages.Count - nextPageIndex);

                            // Get items in parallel
                            readWatch.Start();
                            DataBlock[] blocks = new DataBlock[pageCountThisIteration];
                            Parallel.For(0, pageCountThisIteration, (relativeIndex) =>
                            {
                                try
                                {
                                    // Read the next page of items
                                    Console.Write("[");
                                    blocks[relativeIndex] = provider.GetItemBlock(pages[nextPageIndex + relativeIndex], this.ColumnNames);
                                }
                                catch (Exception e)
                                {
                                    exceptionCount++;
                                    Trace.WriteLine(string.Format("Exception when fetching {0} items. Error: {1}\r\nItem IDs: {2}", ConfigurationName, e.ToString(), String.Join(", ", pages[nextPageIndex + relativeIndex].Select(r => r.ID))));
                                    if (exceptionCount > 10) throw;
                                }
                            });
                            readWatch.Stop();

                            // Append items serially
                            writeWatch.Start();
                            for (int relativeIndex = 0; relativeIndex < pageCountThisIteration; ++relativeIndex)
                            {
                                try
                                {
                                    // Write the next page of items
                                    Console.Write("]");
                                    consumer.Append(blocks[relativeIndex]);

                                    // Track total count appended
                                    itemCount += blocks[relativeIndex].RowCount;

                                    // Track last changed date written
                                    DateTime latestCutoffInGroup = pages[nextPageIndex + relativeIndex].Max(ii => ii.ChangedDate);
                                    if (latestCutoffInGroup > lastChangedItemAppended)
                                    {
                                        lastChangedItemAppended = latestCutoffInGroup;
                                    }
                                }
                                catch (Exception e)
                                {
                                    exceptionCount++;
                                    Trace.WriteLine(string.Format("Exception when writing {0} items. Error: {1}\r\nItem IDs: {2}", ConfigurationName, e.ToString(), String.Join(", ", pages[nextPageIndex + relativeIndex].Select(r => r.ID))));
                                    if (exceptionCount > 10) throw;
                                }
                            }
                            writeWatch.Stop();

                            // Save table if enough time has elapsed
                            if (sinceLastWrite.Elapsed.TotalMinutes > WriteAfterMinutes)
                            {
                                Console.WriteLine();

                                try
                                {
                                    Save(consumer, saveWatch, lastChangedItemAppended);
                                    sinceLastWrite.Restart();
                                }
                                catch (Exception e)
                                {
                                    exceptionCount++;
                                    Trace.WriteLine(string.Format("Exception saving {0} batch. Error: {1}", ConfigurationName, e.ToString()));

                                    if (exceptionCount > 10) throw;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Trace.WriteLine(String.Format("Crawler Failed. At {1:u}, {2:n0} items, {3} read, {4} write, {5} save for '{0}'.", this.ConfigurationName, DateTime.Now, itemCount, readWatch.Elapsed.ToFriendlyString(), writeWatch.Elapsed.ToFriendlyString(), saveWatch.Elapsed.ToFriendlyString()));
                            throw;
                        }
                    }

                    Console.WriteLine();
                }
            }
            finally
            {
                // Disconnect from the source
                if (provider != null)
                {
                    provider.Dispose();
                    provider = null;
                }

                // Save (if any items were added) and disconnect from the consumer
                if (consumer != null)
                {
                    if (itemCount > 0)
                    {
                        Save(consumer, saveWatch, lastChangedItemAppended);
                    }

                    consumer.Dispose();
                    consumer = null;
                }

                Console.WriteLine();

                // Old tracing logic
                Trace.WriteLine(String.Format("Crawler Done. At {1:u}, {2:n0} items, {3} read, {4} write, {5} save for '{0}'.", this.ConfigurationName, DateTime.Now, itemCount, readWatch.Elapsed.ToFriendlyString(), writeWatch.Elapsed.ToFriendlyString(), saveWatch.Elapsed.ToFriendlyString()));
            }
        }

        private void Save(IItemConsumer consumer, Stopwatch saveWatch, DateTime lastCutoffWritten)
        {
            // Save the data itself
            Trace.WriteLine("Saving...");
            saveWatch.Start();
            consumer.Save();
            saveWatch.Stop();
            Trace.WriteLine("Save Complete.");

            // Record the new last cutoff written
            ItemProviderUtilities.SaveLastCutoff(this.Configuration.ArribaTable, this.ConfigurationName, lastCutoffWritten);
        }
    }
}
