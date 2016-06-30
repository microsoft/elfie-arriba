// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Arriba.Client;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Structures;
using Arriba.Types;

namespace Arriba.ClientExample
{
    internal class Program
    {
        private const string ArribaServerUrl = "http://localhost:42784";

        private static void Usage()
        {
            Trace.WriteLine(@"Arriba.ClientExample can run through an end-to-end sample or manage the Scratch table.
  NOTE: Arriba.Server or Arriba.IIS must be running for the client to connect to.
  NOTE: You must run Arriba.Server elevated (or from elevated VS) for it to open a port successfully.

Usage:
  Arriba.ClientExample example
    Run through the end-to-end example test.

  Arriba.ClientExample create
    Create the scratch table according to the settings in the Scratch config.json.

  Arriba.ClientExample drop
    Drop the scratch table and delete the saved copy of it.

  Arriba.ClientExample import ..\Values.csv
    Import the CSV '..\Values.csv' into the Scratch table. [JSON also supported]
    A unique ID column must be present.
    The first column name ending with 'ID' or the first column will be used.
    Columns will be dynamically added.
    
  Arriba.ClientExample check Table
    Check the 'Table' table in the Production DiskCache for consistency.");
        }

        private static int Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Usage();
                return -1;
            }

            string mode = args[0].ToLowerInvariant();

            try
            {
                switch (mode)
                {
                    case "example":
                        Example().Wait();
                        break;
                    case "create":
                        CreateScratchTable().Wait();
                        break;
                    case "drop":
                        DropScratchTable().Wait();
                        break;
                    case "import":
                        if (args.Length < 2)
                        {
                            Usage();
                            return -1;
                        }

                        ImportFileToScratchTable(args[1]).Wait();
                        break;
                    case "importfiles":
                        if (args.Length < 2)
                        {
                            Usage();
                            return -1;
                        }

                        ImportFileMetadataToScratchTable(args[1]).Wait();
                        break;
                    case "check":
                        if (args.Length < 2)
                        {
                            Usage();
                            return -1;
                        }

                        CheckConsistency(args[1]);
                        break;
                    default:
                        Trace.WriteLine("Unrecognized mode '{0}' passed. Run without arguments for usage.", mode);
                        return -1;
                }
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

                Console.WriteLine(ex.Message);
                return -2;
            }

            return 0;
        }

        private static void CheckConsistency(string tableName)
        {
            Trace.WriteLine(String.Format("Checking '{0}' table consistency...", tableName));

            Table t = new Table();
            t.Load(tableName);

            ExecutionDetails details = new ExecutionDetails();
            t.VerifyConsistency(VerificationLevel.Normal, details);

            if (details.Succeeded)
            {
                Trace.WriteLine(String.Format("Done. '{0}' is consistent.", tableName));
            }
            else
            {
                Trace.WriteLine(String.Format("Done. '{0}' has consistency problems.\r\n{1}\r\n{2}", tableName, details.Errors, details.Warnings));
            }
        }

        private static async Task CreateScratchTable()
        {
            Trace.WriteLine("Creating Scratch table...");

            using (var client = new ArribaClient(ArribaServerUrl))
            {
                if (client.Tables.Contains("Scratch"))
                {
                    await client["Scratch"].DeleteAsync();
                }

                CreateTableRequest tableRequest = new CreateTableRequest("Scratch", 250000);
                var table = await client.CreateTableAsync(tableRequest);

                await table.SaveAsync();
                Trace.WriteLine("Done. Scratch table created.");
            }
        }

        private static async Task ImportFileToScratchTable(string filePath)
        {
            using (var client = new ArribaClient(ArribaServerUrl))
            {
                // Create the table if it doesn't yet exist
                if (!client.Tables.Contains("Scratch")) await CreateScratchTable();

                Trace.WriteLine(String.Format("Importing '{0}' into Scratch table...", filePath));

                ArribaTableClient table = client["Scratch"];

                // Import the new CSV
                await table.ImportFileAsync(filePath);
                await table.SaveAsync();
                Trace.WriteLine(String.Format("'{0}' imported into Scratch table.", filePath));
            }
        }

        private static async Task ImportFileMetadataToScratchTable(string basePath)
        {
            using (var client = new ArribaClient(ArribaServerUrl))
            {
                if (!Directory.Exists(basePath))
                {
                    Trace.WriteLine(String.Format("Error: Can't import file metadata from '{0}' because '{0}' was not found.", basePath));
                    return;
                }

                // Create the table if it doesn't yet exist
                if (!client.Tables.Contains("Scratch")) await CreateScratchTable();

                Trace.WriteLine(String.Format("Importing file metadata from '{0}' into Scratch table...", basePath));

                // Build a DataBlock with file details
                DirectoryInfo root = new DirectoryInfo(basePath);
                FileInfo[] files = root.GetFiles("*.*", SearchOption.AllDirectories);

                // Copy values into strongly typed arrays [performance; avoids boxing on insert]
                string[] names = new string[files.Length];
                DateTime[] createdDateUtcs = new DateTime[files.Length];
                DateTime[] modifiedDateUtcs = new DateTime[files.Length];
                long[] lengths = new long[files.Length];

                for (int i = 0; i < files.Length; ++i)
                {
                    FileInfo file = files[i];
                    names[i] = file.Name;
                    createdDateUtcs[i] = file.CreationTimeUtc;
                    modifiedDateUtcs[i] = file.LastWriteTimeUtc;
                    lengths[i] = file.Length;
                }

                // Put data into DataBlock, using existing arrays
                DataBlock block = new DataBlock(new string[] { "Name", "CreatedDateUTC", "ModifiedDateUTC", "LengthBytes" }, files.Length, new Array[] { names, createdDateUtcs, modifiedDateUtcs, lengths });

                // Import the new CSV
                ArribaTableClient table = client["Scratch"];
                await table.ImportDataBlock(block);
                await table.SaveAsync();
                Trace.WriteLine(String.Format("Done. {0:n0} items imported.", files.Length));
            }
        }

        private static async Task DropScratchTable()
        {
            using (var client = new ArribaClient(ArribaServerUrl))
            {
                await client["Scratch"].DeleteAsync();
                Trace.WriteLine("Done. Scratch table dropped.");
            }
        }

        private static async Task Example()
        {
            using (var client = new ArribaClient(ArribaServerUrl))
            {
                // Create Table [2 Partition Bits = 4 Partitions = 256k items]
                CreateTableRequest tableRequest = new CreateTableRequest("Test_" + DateTime.Now.Ticks.ToString(), 2);
                tableRequest.Columns.Add(new ColumnDetails("ID", "int", -1, String.Empty, true));
                tableRequest.Columns.Add(new ColumnDetails("Name", "string", null));
                tableRequest.Columns.Add(new ColumnDetails("Age", "int", -1));
                var table = await client.CreateTableAsync(tableRequest);

                // Add columns (after creation)
                await table.AddColumnsAsync(new ColumnDetails[] { new ColumnDetails("Team", "string", null) });

                // Add items (CSV) [ID,Name,Age]
                using (MemoryStream ms = BuildSampleUserCsv())
                {
                    await table.ImportFileAsync(ms, "csv");
                }

                // Add items (DataBlock)
                DataBlock block = new DataBlock(
                    new string[] { "ID", "Name", "Age", "Team" },
                    1,
                    new Array[] {
                        new int[] { 4 },
                        new string[] { "Karl" },
                        new int[] { 30 },
                        new string[] {  "VSPlat" }
                    });
                await table.ImportDataBlock(block);

                // Get Table Information 
                var d = await table.GetTableInformationAsync();

                // Select ID, Name WHERE Age = 32
                SelectResult selectResult = await table.Select(new SelectQuery(new string[] { "ID" }, "Age = 32"));
                Trace.WriteLine(String.Format("Found {0:n0} 32 year olds (expected 2)", selectResult.Total));

                // Aggregate COUNT(*) WHERE {ALL} BY Age < 30, Age >= 30
                AggregationQuery aggregateQuery = new AggregationQuery("Count", new string[] { "ID" }, "");
                aggregateQuery.Dimensions.Add(new AggregationDimension("Age", "Age < 30", "Age >= 30"));
                AggregationResult aggregateResult = await table.Aggregate(aggregateQuery);
                Trace.WriteLine(String.Format("Found {0:n0} under 30 year olds (expected 1)", aggregateResult.Values[0, 1]));
                Trace.WriteLine(String.Format("Found {0:n0} over 30 year olds (expected 3)", aggregateResult.Values[1, 1]));
                Trace.WriteLine(String.Format("Found {0:n0} altogether (expected 4)", aggregateResult.Values[2, 1]));

                // Delete WHERE ID = 2
                int countDeleted = await table.Delete(SelectQuery.ParseWhere("ID = 2"));
                Trace.WriteLine(String.Format("Deleted {0:n0} users (expected 1)", countDeleted));

                // Select ID, Name WHERE Age = 32 (again)
                selectResult = await table.Select(new SelectQuery(new string[] { "ID" }, "Age = 32"));
                Trace.WriteLine(String.Format("Found {0:n0} 32 year olds (expected 1)", selectResult.Total));

                // Add write permission to another user
                await table.GrantPermissionsAsync(Model.Security.PermissionScope.Reader, Model.Security.IdentityScope.User, "phil");
                SecurityPermissions permissions = await table.GetPermissionsAsync();
                Trace.WriteLine(String.Format("Expecting 'Phil' as reader. Readers: {0}", permissions.Readers.First().Name));

                // Deny myself write permission
                await table.RevokePermissionsAsync(Model.Security.PermissionScope.Reader, Model.Security.IdentityScope.User, "phil");
                permissions = await table.GetPermissionsAsync();
                Trace.WriteLine(String.Format("Found {0:n0} readers (expected 0)", permissions.Readers.Count()));

                // Delete table
                await table.DeleteAsync();
            }
        }

        private static MemoryStream BuildSampleUserCsv()
        {
            MemoryStream ms = new MemoryStream();
            CsvWriter w = new CsvWriter(new SerializationContext(ms), new string[] { "ID", "Name", "Age", "Team" });
            w.AppendRow(new object[] { 1, "Phil", 29, "VSPlat" });
            w.AppendRow(new object[] { 2, "Scott", 32, "VSPlat" });
            w.AppendRow(new object[] { 3, "Barry", 32, "VSPlat" });

            return ms;
        }

        private static async Task ExampleShort()
        {
            // Connect to local Arriba server
            using (var client = new ArribaClient(ArribaServerUrl))
            {
                // Create Table [2 Partition Bits = 4 Partitions = 256k items]
                CreateTableRequest tableRequest = new CreateTableRequest("Test_" + DateTime.Now.Ticks.ToString(), 2);
                tableRequest.Columns.Add(new ColumnDetails("ID", "int", -1, String.Empty, true));
                tableRequest.Columns.Add(new ColumnDetails("Name", "string", null));
                tableRequest.Columns.Add(new ColumnDetails("Age", "int", -1));
                var table = await client.CreateTableAsync(tableRequest);

                // Add or Update items (CSV) [ID,Name,Age]
                using (MemoryStream ms = BuildSampleUserCsv())
                {
                    await table.ImportFileAsync(ms, "csv");
                }

                // Select ID, Name WHERE Age = 32
                SelectResult selectResult = await table.Select(new SelectQuery(new string[] { "ID" }, "Age = 32"));
                Console.WriteLine("Found {0:n0} 32 year olds (expected 2)", selectResult.Total);

                // Aggregate COUNT(*) WHERE {ALL} BY Age < 30, Age >= 30
                AggregationQuery aggregateQuery = new AggregationQuery("Count", new string[] { "ID" }, "");
                aggregateQuery.Dimensions.Add(new AggregationDimension("Age", "Age < 30", "Age >= 30"));
                AggregationResult aggregateResult = await table.Aggregate(aggregateQuery);
                Console.WriteLine("Found {0:n0} under 30 year olds (expected 1)", aggregateResult.Values[0, 1]);
                Console.WriteLine("Found {0:n0} over 30 year olds (expected 3)", aggregateResult.Values[1, 1]);
                Console.WriteLine("Found {0:n0} altogether (expected 4)", aggregateResult.Values[2, 1]);

                // Delete WHERE Age < 30
                int countDeleted = await table.Delete(SelectQuery.ParseWhere("Age < 30"));
                Console.WriteLine("Deleted {0:n0} users (expected 1)", countDeleted);
            }
        }
    }
}
