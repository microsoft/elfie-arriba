// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Types;
using System;

namespace XForm.IO
{
    public class TableMetadata
    {
        public int RowCount { get; set; }
        public List<ColumnDetails> Schema { get; set; }

        public TableMetadata()
        {
            this.Schema = new List<ColumnDetails>();
        }

        public TableMetadata(int rowCount, List<ColumnDetails> schema)
        {
            this.RowCount = rowCount;
            this.Schema = schema;
        }
    }

    public static class TableMetadataSerializer
    {
        private const string SchemaFileName = "Schema.csv";
        private const string MetadataFileName = "Metadata.csv";

        private static Cache<TableMetadata> s_Cache = new Cache<TableMetadata>(TimeSpan.FromMinutes(1));

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

            s_Cache.Add(tableRootPath, metadata);
        }

        public static TableMetadata Read(IStreamProvider streamProvider, string tableRootPath)
        {
            return s_Cache.GetOrBuild(tableRootPath,
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
                
                while(mr.NextRow())
                {
                    String8 name = mr.Current(nameIndex).ToString8();
                    String8 context = mr.Current(contextIndex).ToString8();
                    ITabularValue value = mr.Current(valueIndex);

                    if(name.Equals("RowCount"))
                    {
                        metadata.RowCount = value.ToInteger();
                    }
                    else
                    {
                        throw new NotImplementedException($"TableMetadataSerializer.Read doesn't know how to read Metadata '{name}'");
                    }
                }
            }

            return metadata;
        }
    }
}
