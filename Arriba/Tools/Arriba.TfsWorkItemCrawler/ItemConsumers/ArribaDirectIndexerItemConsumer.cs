// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Arriba.Diagnostics;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    /// <summary>
    ///  An IItemConsumer which indexes the items in an Arriba table using the
    ///  server API directly. This is good for debugging server side write
    ///  issues or for using Arriba within a single process. When Arriba is
    ///  served via a web service, the ArribaIndexerItemConsumer should be used
    ///  instead so that only one instance of the table needs to exist in memory.
    /// </summary>
    public class ArribaDirectIndexerItemConsumer : IItemConsumer
    {
        public bool DiagnosticsEnabled { get; set; }
        public VerificationLevel DiagnosticsLevel { get; set; }
        private CrawlerConfiguration Configuration { get; set; }

        private Table Table { get; set; }

        public ArribaDirectIndexerItemConsumer(CrawlerConfiguration config)
        {
            this.Configuration = config;

            this.DiagnosticsEnabled = Debugger.IsAttached;
            this.DiagnosticsLevel = VerificationLevel.Normal;
        }

        public void CreateTable(IList<ColumnDetails> columns, SecurityPermissions permissions)
        {
            this.Table = new Table(this.Configuration.ArribaTable, this.Configuration.ItemCountLimit);

            // Try to load the table if it already exists
            if (BinarySerializable.EnumerateUnder(Path.Combine("Tables", this.Configuration.ArribaTable)).Count() > 0)
            {
                Trace.WriteLine(string.Format("Loading Arriba Table '{0}'...", this.Configuration.ArribaTable));
                this.Table.Load(this.Configuration.ArribaTable);
            }

            // Columns are added dynamically by Append

            // Set the table security
            SecureDatabase sdb = new SecureDatabase();
            sdb.SetSecurity(this.Configuration.ArribaTable, permissions);
            sdb.SaveSecurity(this.Configuration.ArribaTable);

            // Debug Only: Verify consistency just after load
            if (this.DiagnosticsEnabled)
            {
                Trace.WriteLine("Verifying Arriba Table consistency [on load]...");

                ExecutionDetails d = new ExecutionDetails();
                this.Table.VerifyConsistency(this.DiagnosticsLevel, d);

                if (!d.Succeeded)
                {
                    Debugger.Break();
                    Trace.TraceError(String.Format("Consistency Errors Detected: {0}", String.Join("\r\n", d.Errors)));
                }
            }
        }

        public void Append(DataBlock items)
        {
            //if (this.DiagnosticsEnabled && Debugger.IsAttached)
            //{
            //    // REPRO: Use to have Arriba run non-parallel to get exception helper
            //    this.Table.RunParallel = false;

            //    // REPRO: Strip to only ID and column showing problem for simpler debugging
            //    items = items.StripToColumns(new string[] { "ID", "Repro Steps" });

            //    // REPRO: Use to break just before a troublesome item
            //    if (items[0, 0].ToString().Equals("721458")) Debugger.Break();
            //}

            this.Table.AddOrUpdate(items, new AddOrUpdateOptions() { AddMissingColumns = true });

            // REPRO: Identify the items causing consistency problems as we go. Too slow for full scale.
            //if (this.DiagnosticsEnabled && Debugger.IsAttached)
            //{
            //    ExecutionDetails d = new ExecutionDetails();
            //    this.Table.VerifyConsistency(this.DiagnosticsLevel, d);
            //    if (!d.Succeeded)
            //    {
            //        Debugger.Break();
            //        Trace.Write(String.Format("Consistency Errors Detected: {0}", String.Join("\r\n", d.Errors)));
            //    }
            //}
        }

        public void Save()
        {
            // Debug Only: Verify consistency just before save
            if (this.DiagnosticsEnabled)
            {
                Trace.WriteLine("Verifying Arriba Table consistency [on save]...");

                ExecutionDetails d = new ExecutionDetails();
                this.Table.VerifyConsistency(this.DiagnosticsLevel, d);

                if (!d.Succeeded)
                {
                    Debugger.Break();
                    Trace.TraceError(String.Format("Consistency Errors Detected: {0}", String.Join("\r\n", d.Errors)));
                }
            }

            this.Table.Save();
        }

        public void Dispose()
        { }
    }
}
