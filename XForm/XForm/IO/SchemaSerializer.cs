// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Types;

namespace XForm.IO
{
    public static class SchemaSerializer
    {
        private const string SchemaFileName = "Schema.csv";

        public static void Write(IStreamProvider streamProvider, string tableRootPath, IEnumerable<ColumnDetails> columns)
        {
            String8Block block = new String8Block();
            using (ITabularWriter schemaWriter = TabularFactory.BuildWriter(streamProvider.OpenWrite(Path.Combine(tableRootPath, SchemaFileName)), SchemaFileName))
            {
                schemaWriter.SetColumns(new string[] { "Name", "Type", "Nullable" });

                foreach (ColumnDetails column in columns)
                {
                    schemaWriter.Write(block.GetCopy(column.Name));
                    schemaWriter.Write(block.GetCopy(column.Type.Name.ToString()));
                    schemaWriter.Write(column.Nullable);
                    schemaWriter.NextRow();
                }
            }
        }

        public static List<ColumnDetails> Read(IStreamProvider streamProvider, string tableRootPath)
        {
            List<ColumnDetails> columns = new List<ColumnDetails>();

            using (ITabularReader reader = TabularFactory.BuildReader(streamProvider.OpenRead(Path.Combine(tableRootPath, SchemaFileName)), SchemaFileName))
            {
                int nameIndex = reader.ColumnIndex("Name");
                int typeIndex = reader.ColumnIndex("Type");
                int nullableIndex = reader.ColumnIndex("Nullable");

                while (reader.NextRow())
                {
                    columns.Add(new ColumnDetails(reader.Current(nameIndex).ToString(), TypeProviderFactory.Get(reader.Current(typeIndex).ToString()).Type, reader.Current(2).ToBoolean()));
                }
            }

            return columns;
        }
    }
}
