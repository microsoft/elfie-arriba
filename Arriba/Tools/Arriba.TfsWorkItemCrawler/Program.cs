using Arriba.Serialization;
using Arriba.TfsWorkItemCrawler.ItemConsumers;
using Arriba.TfsWorkItemCrawler.ItemProviders;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

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
            bool toCsv = (args.Length > 2 && String.Equals(args[2], "ToCsv", StringComparison.OrdinalIgnoreCase));
            bool fromCsv = (args.Length > 2 && String.Equals(args[2], "FromCsv", StringComparison.OrdinalIgnoreCase));
            string changedDateColumnName = (args.Length > 3 ? args[3] : "Changed Date");
            int port = (args.Length > 4 ? int.Parse(args[4]) : 42784);

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

                    // Set Crawler to use same DiskCache path as service will. Needed when using ArribaDirectIndexerItemConsumer
                    // and keeps written CSVs together with the Arriba serialized table.
                    BinarySerializable.CachePath = Path.Combine(BinarySerializable.CachePath, @"..\..\DiskCache");

                    // Load the Configuration
                    string configJsonPath = String.Format(@"..\..\Bung.Web\databases\{0}\config.json", configurationName);
                    string configJson = File.ReadAllText(configJsonPath);
                    CrawlerConfiguration config = JsonConvert.DeserializeObject<CrawlerConfiguration>(configJson);

                    // Password storage mode
                    if (mode.Equals("-password", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Write("Enter TFS Online Password to local user encrypt: ");
                        string password = Console.ReadLine();
                        if (String.IsNullOrEmpty(password)) return -1;

                        string encryptedPassword = TfsItemProvider.LocalUserEncryptPassword(password);
                        File.WriteAllText(config.TfsOnlineEncryptedPasswordFilePath, encryptedPassword);

                        Console.WriteLine("Encrypted Password written to '{0}'. Run Crawler to test.", config.TfsOnlineEncryptedPasswordFilePath);
                        Console.WriteLine();
                        return 0;
                    }

                    // Build the item consumer
                    IItemConsumer consumer = null;

                    if (toCsv)
                    {
                        // To CSV instead of Arriba if requested
                        consumer = new CsvWriterItemConsumer(configurationName, changedDateColumnName);
                    }
                    else
                    {
                        consumer = new ArribaClientIndexerItemConsumer(config, String.Format("http://localhost:{0}", port));
                    }
                    
                    if (fromCsv)
                    {
                        // Csv Import mode - use CsvImporter to read everything in Date range from CSVs and import
                        CsvImporter importer = new CsvImporter(config, configurationName, changedDateColumnName);
                        importer.Import(consumer);
                    }
                    else
                    {
                        // Normal crawl - build the normal item provider 
                        IItemProvider provider = ItemProviderUtilities.Build(config);

                        // Build a crawler and crawl the items in restartable order
                        DefaultCrawler crawler = new DefaultCrawler(config, configurationName, !mode.Equals("-i"));
                        crawler.Crawl(provider, consumer);
                    }

                    return 0;
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
     'Arriba.TfsWorkItemCrawler DevDiv -i' -> Append updated DevDiv items from primary provider.
     'Arriba.TfsWorkItemCrawler DevDiv -r' -> Rebuild all DevDiv items from primary provider.
     'Arriba.TfsWorkItemCrawler DevDiv -password -> Local User Encrypt a TFS online password for config.
");
        }

    }
}
