// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Arriba.Client;
using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Structures;
using Arriba.Types;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    /// <summary>
    ///  An IItemConsumer which indexes items into an Arriba table using the
    ///  Arriba client API. This consumer should be used when hosting Arriba with
    ///  the service running, so that the one live copy hosting searches is used
    ///  to index changes also.
    /// </summary>
    public class ArribaClientIndexerItemConsumer : IItemConsumer
    {
        private string ServiceUrl { get; set; }
        private CrawlerConfiguration Configuration { get; set; }

        private ArribaClient Client { get; set; }
        private ArribaTableClient Table { get; set; }

        private Stopwatch SinceLastWrite { get; set; }

        public ArribaClientIndexerItemConsumer(CrawlerConfiguration config, string serviceUrl)
        {
            this.Configuration = config;
            this.ServiceUrl = serviceUrl;

            // Allow long timeouts (save for huge databases is slow)
            this.Client = new ArribaClient(new Uri(this.ServiceUrl), TimeSpan.FromMinutes(15));
            this.Table = this.Client[this.Configuration.ArribaTable];
        }

        public void CreateTable(IList<ColumnDetails> columns, SecurityPermissions permissions)
        {
            HashSet<string> tables = new HashSet<string>(this.Client.Tables);
            if (!tables.Contains(this.Configuration.ArribaTable))
            {
                // Create the table if it doesn't yet exist
                CreateTableRequest ctr = new CreateTableRequest();
                ctr.TableName = this.Configuration.ArribaTable;
                ctr.ItemCountLimit = this.Configuration.ItemCountLimit;
                ctr.Permissions = permissions;

                this.Client.CreateTableAsync(ctr).Wait();
            }
            else
            {
                // Always ensure permissions up-to-date on Table
                this.Table.SetPermissionsAsync(permissions).Wait();
            }

            // Columns will be added dynamically
        }

        public void Append(DataBlock items)
        {
            this.Table.ImportDataBlock(items).Wait();
        }

        public void Save()
        {
            this.Table.SaveAsync().Wait();
        }

        public void Dispose()
        {
            if (this.Table != null)
            {
                this.Table = null;
            }

            if (this.Client != null)
            {
                this.Client.Dispose();
                this.Client = null;
            }
        }
    }
}
