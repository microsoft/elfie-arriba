// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Column;
using Arriba.Structures;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arriba.Client.Serialization.Json
{
    /// <summary>
    /// JSON Serializer for Arriba Data Blocks. 
    /// </summary>
    public class DataBlockJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DataBlock).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            int rowCount = jObject["rowCount"].Value<int>();
            List<ColumnDetails> columns = new List<ColumnDetails>();

            foreach (JToken row in jObject["columns"].Children())
            {
                columns.Add(row.ToObject<ColumnDetails>());
            }

            DataBlock block = new DataBlock(columns, rowCount);

            int rowIndex = 0;
            int colIndex = 0;

            foreach (JToken row in jObject["rows"].Children())
            {
                foreach (JToken value in row.Children())
                {
                    block[rowIndex, colIndex] = Value.Create(value.ToString());
                    colIndex++;
                }

                rowIndex++;
                colIndex = 0;
            }

            return block;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DataBlock block = (DataBlock)value;

            writer.WriteStartObject();
            writer.WritePropertyName("columnCount");
            writer.WriteValue(block.ColumnCount);
            writer.WritePropertyName("rowCount");
            writer.WriteValue(block.RowCount);

            writer.WritePropertyName("columns");
            writer.WriteStartArray();
            foreach (ColumnDetails column in block.Columns)
            {
                serializer.Serialize(writer, column);
            }
            writer.WriteEndArray();

            writer.WritePropertyName("rows");
            writer.WriteStartArray();
            for (int row = 0; row < block.RowCount; ++row)
            {
                writer.WriteStartArray();
                for (int column = 0; column < block.ColumnCount; ++column)
                {
                    object cell = block[row, column];
                    if (cell is ByteBlock) cell = ((ByteBlock)cell).ToString();

                    serializer.Serialize(writer, cell);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
