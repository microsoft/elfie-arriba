using Arriba.Structures;
using Arriba.TfsWorkItemCrawler.ItemConsumers;
using Arriba.TfsWorkItemCrawler.ItemProviders;
using System;
using System.Diagnostics;

namespace Arriba.TfsWorkItemCrawler
{
    class CsvImporter
    {
        private const int BatchSize = 100;
        private const int WriteAfterMinutes = 20;

        private string ConfigurationName { get; set; }
        private CrawlerConfiguration Configuration { get; set; }
        private string ChangedDateColumn { get; set; }

        public CsvImporter(CrawlerConfiguration config, string configurationName, string changedDateColumn)
        {
            this.Configuration = config;
            this.ConfigurationName = configurationName;
            this.ChangedDateColumn = changedDateColumn;
        }

        public void Import(IItemConsumer consumer)
        {
            DateTime lastCutoffWritten = ItemProviderUtilities.LoadLastCutoff(this.Configuration.ArribaTable, this.ConfigurationName + ".CSV", false);
            Stopwatch saveWatch = null;

            CsvReaderItemProvider provider = null;

            try
            {
                provider = new CsvReaderItemProvider(this.Configuration.ArribaTable, this.ChangedDateColumn, lastCutoffWritten, DateTime.UtcNow);

                while (true)
                {
                    // Get another batch of items
                    Console.Write("[");
                    DataBlock block = provider.GetNextBlock(BatchSize);
                    if (block == null || block.RowCount == 0) break;

                    // Append them
                    Console.Write("]");
                    consumer.Append(block);

                    // Track the last item changed date
                    DateTime lastItemInBlock;
                    Value.Create(block[block.RowCount - 1, block.IndexOfColumn(this.ChangedDateColumn)]).TryConvert<DateTime>(out lastItemInBlock);
                    if (lastItemInBlock > lastCutoffWritten)
                    {
                        lastCutoffWritten = lastItemInBlock;
                    }

                    if (saveWatch == null) saveWatch = Stopwatch.StartNew();
                    if (saveWatch.Elapsed.TotalMinutes > WriteAfterMinutes)
                    {
                        Save(consumer, lastCutoffWritten);
                        saveWatch.Restart();
                    }
                }
            }
            finally
            {
                provider.Dispose();

                Save(consumer, lastCutoffWritten);
                consumer.Dispose();
            }
        }

        private void Save(IItemConsumer consumer, DateTime lastCutoffWritten)
        {
            Trace.WriteLine("Saving...");
            consumer.Save();
            Trace.WriteLine("Save Complete.");

            // Record the new last cutoff written
            ItemProviderUtilities.SaveLastCutoff(this.Configuration.ArribaTable, this.ConfigurationName + ".CSV", lastCutoffWritten);
        }
    }
}
