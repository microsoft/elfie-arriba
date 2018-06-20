// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.Extensions;
using XForm.IO.StreamProvider;
using XForm.Types;

namespace XForm.IO
{
    public class TableMetadata
    {
        public long RowCount { get; set; }
        public string Query { get; set; }
        public List<ColumnDetails> Schema { get; set; }
        public List<string> Partitions { get; set; }

        public TableMetadata()
        {
            this.Schema = new List<ColumnDetails>();
            this.Partitions = new List<string>();
        }

        public TableMetadata(int rowCount, List<ColumnDetails> schema)
        {
            this.RowCount = rowCount;
            this.Schema = schema;
            this.Partitions = new List<string>();
        }
    }

    public static class TableMetadataSerializer
    {
        private const string SchemaFileName = "Schema.csv";
        private const string MetadataFileName = "Metadata.csv";
        private const string PartitionsFileName = "Partitions.csv";
        private const string ConfigQueryPath = "Config.xql";

        private static Cache<TableMetadata> s_Cache = new Cache<TableMetadata>();

        public static void Write(IStreamProvider streamProvider, string tableRootPath, TableMetadata metadata)
        {
            String8Block block = new String8Block();
            using (ITabularWriter sw = TabularFactory.BuildWriter(streamProvider.OpenWrite(Path.Combine(tableRootPath, SchemaFileName)), SchemaFileName))
            {
                sw.SetColumns(new string[] { "Name", "Type" });

                foreach (ColumnDetails column in metadata.Schema)
                {
                    sw.Write(block.GetCopy(column.Name));
                    sw.Write(block.GetCopy(column.Type.Name.ToString()));
                    sw.NextRow();
                }
            }

            using (ITabularWriter mw = TabularFactory.BuildWriter(streamProvider.OpenWrite(Path.Combine(tableRootPath, MetadataFileName)), MetadataFileName))
            {
                mw.SetColumns(new string[] { "Name", "Context", "Value" });

                mw.Write(block.GetCopy("RowCount"));
                mw.Write(String8.Empty);
                mw.Write(metadata.RowCount);
                mw.NextRow();
            }

            streamProvider.WriteAllText(Path.Combine(tableRootPath, ConfigQueryPath), metadata.Query);

            if (metadata.Partitions.Count > 0)
            {
                using (ITabularWriter pw = TabularFactory.BuildWriter(streamProvider.OpenWrite(Path.Combine(tableRootPath, PartitionsFileName)), PartitionsFileName))
                {
                    pw.SetColumns(new string[] { "Name" });

                    foreach(string partition in metadata.Partitions)
                    {
                        pw.Write(block.GetCopy(partition));
                        pw.NextRow();
                    }
                }
            }

            s_Cache.Add($"{streamProvider}|{tableRootPath}", metadata);
        }

        public static TableMetadata Read(IStreamProvider streamProvider, string tableRootPath)
        {
            return s_Cache.GetOrBuild($"{streamProvider}|{tableRootPath}",
                () => streamProvider.Attributes(Path.Combine(tableRootPath, MetadataFileName)).WhenModifiedUtc,
                () => Build(streamProvider, tableRootPath));
        }

        private static TableMetadata Build(IStreamProvider streamProvider, string tableRootPath)
        {
            TableMetadata metadata = new TableMetadata();
            string schemaFilePath = Path.Combine(tableRootPath, SchemaFileName);

            using (ITabularReader sr = TabularFactory.BuildReader(streamProvider.OpenRead(schemaFilePath), SchemaFileName))
            {
                int nameIndex = sr.ColumnIndex("Name");
                int typeIndex = sr.ColumnIndex("Type");

                while (sr.NextRow())
                {
                    metadata.Schema.Add(new ColumnDetails(sr.Current(nameIndex).ToString(), TypeProviderFactory.Get(sr.Current(typeIndex).ToString()).Type));
                }
            }

            using (ITabularReader mr = TabularFactory.BuildReader(streamProvider.OpenRead(Path.Combine(tableRootPath, MetadataFileName)), MetadataFileName))
            {
                int nameIndex = mr.ColumnIndex("Name");
                int contextIndex = mr.ColumnIndex("Context");
                int valueIndex = mr.ColumnIndex("Value");

                while (mr.NextRow())
                {
                    String8 name = mr.Current(nameIndex).ToString8();
                    String8 context = mr.Current(contextIndex).ToString8();
                    ITabularValue value = mr.Current(valueIndex);

                    if (name.Equals("RowCount"))
                    {
                        long rowCount;
                        if(value.ToString8().TryToLong(out rowCount)) metadata.RowCount = rowCount;
                    }
                    else
                    {
                        throw new NotImplementedException($"TableMetadataSerializer.Read doesn't know how to read Metadata '{name}'");
                    }
                }
            }

            metadata.Query = streamProvider.ReadAllText(Path.Combine(tableRootPath, ConfigQueryPath));

            string partitionsPath = Path.Combine(tableRootPath, PartitionsFileName);
            if(streamProvider.Attributes(partitionsPath).Exists)
            {
                using (ITabularReader pr = TabularFactory.BuildReader(streamProvider.OpenRead(partitionsPath), PartitionsFileName))
                {
                    int nameIndex = pr.ColumnIndex("Name");

                    while (pr.NextRow())
                    {
                        metadata.Partitions.Add(pr.Current(nameIndex).ToString());
                    }
                }
            }

            return metadata;
        }

        public static bool UncachedExists(IStreamProvider streamProvider, string tableRootPath)
        {
            return streamProvider.UncachedExists(Path.Combine(tableRootPath, MetadataFileName));
        }

        public static void Delete(IStreamProvider streamProvider, string tableRootPath)
        {
            // Delete metadata first (the main marker we use to detect table presence)
            streamProvider.Delete(Path.Combine(tableRootPath, MetadataFileName));

            // Delete the table 
            streamProvider.DeleteWithRetries(tableRootPath);
        }
    }
}
