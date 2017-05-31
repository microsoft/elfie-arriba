// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Arriba.TfsWorkItemCrawler.ItemProviders;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

using Newtonsoft.Json;

namespace Arriba.TfsWorkItemCrawler
{
    internal class ProductStudioHistoryJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ProductStudioFullHistory).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ProductStudioFullHistory fullHistory = (ProductStudioFullHistory)value;

            writer.WriteStartArray();

            IEnumerable<ProductStudioChangeHistory> changes = from change in fullHistory.ProductStudioChangeHistoryRecords
                                                              orderby change.AddedDate descending
                                                              select change;

            foreach (ProductStudioChangeHistory change in changes)
            {
                if (!String.IsNullOrWhiteSpace(change.Value))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("when");
                    writer.WriteValue(change.AddedDate.ToUniversalTime().ToString("u"));

                    writer.WritePropertyName("who");
                    writer.WriteValue(change.ChangedBy ?? String.Empty);

                    writer.WritePropertyName("comment");
                    writer.WriteValue(change.Value);

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
    }

    public class ProductStudioFullHistory
    {
        private static XmlSerializer s_xmlSerializer;

        [XmlArrayItem(ElementName = "ChangeHistory")]
        public List<ProductStudioChangeHistory> ProductStudioChangeHistoryRecords { get; set; }

        public ProductStudioFullHistory()
        {
            this.ProductStudioChangeHistoryRecords = new List<ProductStudioChangeHistory>();
        }

        static ProductStudioFullHistory()
        {
            s_xmlSerializer = new XmlSerializer(typeof(ProductStudioFullHistory));
        }

        public static ProductStudioFullHistory LoadFullHistory(string rawHistory)
        {
            if (String.IsNullOrWhiteSpace(rawHistory)) { return new ProductStudioFullHistory(); }

            Debug.Assert(s_xmlSerializer != null, "xmlSerializer must be initialized in the static constructor.");

            //Comes from database as ChangeHistroy entries with no root node. Need to wrap in root node to deserialize with XmlSerializer.
            string rawHistoryXml = String.Format("<ProductStudioFullHistory><ProductStudioChangeHistoryRecords>{0}</ProductStudioChangeHistoryRecords></ProductStudioFullHistory>", rawHistory);

            using (TextReader reader = new StringReader(rawHistoryXml))
            {
                try
                {
                    ProductStudioFullHistory hist = (ProductStudioFullHistory)s_xmlSerializer.Deserialize(reader);

                    foreach (ProductStudioChangeHistory change in hist.ProductStudioChangeHistoryRecords)
                    {
                        change.Value = ItemProviderUtilities.ConvertLineBreaksToHtml(change.Value);
                    }

                    return hist;
                }
                catch (InvalidOperationException)
                {
                    //There was data in the field, but it couldn't be deserialized as ProductStudioChangeHistory records.
                    //We still want the data so create a single ChangeHistory record and return the entire contents of this
                    //as the value.
                    ProductStudioFullHistory hist = new ProductStudioFullHistory();
                    hist.ProductStudioChangeHistoryRecords.Add(new ProductStudioChangeHistory() { Value = rawHistory });
                    return hist;
                }
            }
        }
    }

    public class ProductStudioChangeHistory
    {
        [XmlAttribute(AttributeName = "AddedDate")]
        public DateTime AddedDate { get; set; }

        [XmlAttribute(AttributeName = "ChangedBy")]
        public string ChangedBy { get; set; }

        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
    }
}
