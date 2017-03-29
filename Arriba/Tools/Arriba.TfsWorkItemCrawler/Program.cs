using Arriba.Model.Column;
using Arriba.TfsWorkItemCrawler.ItemConsumers;
using Arriba.TfsWorkItemCrawler.ItemProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Arriba.TfsWorkItemCrawler
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Usage();
                return -1;
            }

            string configurationName = args[0];
            string mode = args[1].ToLowerInvariant();

            using (FileLock locker = FileLock.TryGet(String.Format("Arriba.TfsWorkItemCrawler.{0}.lock", configurationName)))
            {
                try
                {
                    // Ensure we got the file lock (no duplicate crawlers
                    if (locker == null)
                    {
                        Console.WriteLine("Another instance running. Stopping.");
                        return -2;
                    }

                    // Load the Configuration
                    string configJsonPath = String.Format(@"..\..\Databases\{0}\config.json", configurationName);
                    string configJson = File.ReadAllText(configJsonPath);

                    CrawlerConfiguration config = JsonConvert.DeserializeObject<CrawlerConfiguration>(configJson);
                    config.ConfigurationName = configurationName;

                    // Password storage mode
                    if (mode.Equals("-password", StringComparison.OrdinalIgnoreCase))
                    {
                        return TfsItemProvider.EncryptPassword(config);
                    }

                    // Build the item consumer
                    IItemConsumer consumer;
                    if (config.UseDirectConsumer)
                    {
                        consumer = new ArribaDirectIndexerItemConsumer(config);
                    }
                    else
                    {
                        consumer = new ArribaClientIndexerItemConsumer(config, config.ArribaServiceUrl ?? "http://localhost:42784");
                    }

                    // Build the item provider
                    IItemProvider provider = ItemProviderUtilities.Build(config);

                    // Determine the list of columns to crawl
                    List<ColumnDetails> columnsToAdd = new List<ColumnDetails>(provider.GetColumns().Where(cd => !config.ColumnsToExclude.Contains(cd.Name)));

                    // Create the target table (if it doesn't already exist)
                    consumer.CreateTable(columnsToAdd, config.LoadPermissions());

                    // Build a crawler and crawl the items in restartable order
                    DefaultCrawler crawler = new DefaultCrawler(config, columnsToAdd.Select((cd) => cd.Name), configurationName, !mode.Equals("-i"));
                    crawler.Crawl(provider, consumer);

                    return 0;
                }
                catch (AggregateException ex)
                {
                    foreach(Exception inner in ex.InnerExceptions)
                    {
                        Trace.TraceError(String.Format("ERROR: {0}\r\n{1}", Environment.CommandLine, inner));
                    }

                    return -2;
                }
                catch (Exception ex)
                {
                    Trace.TraceError(String.Format("ERROR: {0}\r\n{1}", Environment.CommandLine, ex));
                    return -2;
                }
            }
        }

        static void Usage()
        {
            Console.WriteLine(
@" Usage: Arriba.TfsWorkItemCrawler <configName> <mode> [<modeArguments>]
     'Arriba.TfsWorkItemCrawler MyDatabase -i' -> Append updated MyDatabase items from primary provider.
     'Arriba.TfsWorkItemCrawler MyDatabase -r' -> Rebuild all MyDatabase items from primary provider.
     'Arriba.TfsWorkItemCrawler MyDatabase -password -> Local User Encrypt a TFS online password for config.
");
        }

    }
}
