// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

using Newtonsoft.Json;

namespace Arriba.TfsWorkItemCrawler
{
    /// <summary>
    ///  JsonConverter for Microsoft.TeamFoundation.WorkItemTracking.Client.RevisionCollection.
    /// </summary>
    internal class RevisionCollectionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(RevisionCollection).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            RevisionCollection revisions = (RevisionCollection)value;

            writer.WriteStartArray();

            // Walk in reverse order to make latest first by default
            for (int i = revisions.Count - 1; i >= 0; --i)
            {
                Revision revision = revisions[i];
                string history = (string)revision.Fields["History"].Value;

                // Only add revisions with nonempty comments
                if (!String.IsNullOrEmpty(history))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("when");
                    writer.WriteValue(((DateTime)revision.Fields["Changed Date"].Value).ToUniversalTime().ToString("u"));

                    writer.WritePropertyName("who");
                    writer.WriteValue(revision.Fields["Changed By"].Value);

                    writer.WritePropertyName("comment");
                    writer.WriteValue(history);

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
    }
}
