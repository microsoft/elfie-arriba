// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

using Arriba.Serialization;
using Arriba.Serialization.Csv;

namespace Arriba.Monitoring
{
    public class CsvEventConsumer : BatchBufferedEventConsumer
    {
        private const int BatchSize = 100;
        private static string[] s_columnNames = new string[] { "TimeStamp", "OpCode", "Level", "EntityType", "EntityIdentity", "Name", "User", "Detail", "Source", "RuntimeMilliseconds" };

        private string CsvFolderPath { get; set; }

        public CsvEventConsumer(string folderPath = null) : base(false, BatchSize)
        {
            this.CsvFolderPath = folderPath ?? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Logs");
            Directory.CreateDirectory(this.CsvFolderPath);
        }

        public override MonitorEventLevel NotifyOnEventFlags
        {
            get
            {
                return MonitorEventLevel.Information | MonitorEventLevel.Error | MonitorEventLevel.Warning;
            }
        }

        public override MonitorEventOpCode NotifyOnOpCodeFlags
        {
            get
            {
                return MonitorEventOpCode.Mark | MonitorEventOpCode.Stop;
            }
        }

        protected override void OnBatch(MonitorEventEntry[] events)
        {
            using (CsvWriter writer = BuildWriter())
            {
                foreach (MonitorEventEntry e in events)
                {
                    writer.AppendValue(e.TimeStamp);
                    writer.AppendValue(e.OpCode);
                    writer.AppendValue(e.Level);
                    writer.AppendValue(e.EntityType);
                    writer.AppendValue(e.EntityIdentity);
                    writer.AppendValue(e.Name);
                    writer.AppendValue(e.User);
                    writer.AppendValue(e.Detail);
                    writer.AppendValue(e.Source);
                    writer.AppendValue(e.RuntimeMilliseconds);
                    writer.AppendRowSeparator();
                }
            }
        }

        private CsvWriter BuildWriter()
        {
            string csvFilePath = Path.Combine(this.CsvFolderPath, String.Format("{0}.csv", DateTime.Now.ToString("yyyy-MM-dd")));

            // Open a CSV for the current date. Fallback to alternate names if columns have changed and try again
            Exception lastException = null;
            for (int attempt = 1; attempt <= 10; ++attempt)
            {
                try
                {
                    return new CsvWriter(new SerializationContext(new FileStream(csvFilePath, FileMode.OpenOrCreate)), s_columnNames);
                }
                catch (IOException ex)
                {
                    // If CSV is invalid or columns have changed, create a separate one
                    lastException = ex;
                    csvFilePath = Path.Combine(this.CsvFolderPath, String.Format("{0}.{1}.csv", DateTime.Now.ToString("yyyy-MM-dd"), attempt));
                }
            }

            throw lastException;
        }
    }
}
