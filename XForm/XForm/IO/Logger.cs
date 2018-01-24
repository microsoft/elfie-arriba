// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.IO.StreamProvider;

namespace XForm.IO
{
    public enum MessageType
    {
        Query,
        Source,
        Timing,
        Detail,
        Warning,
        AssertFailed
    }

    public interface ILogger : IDisposable
    {
        void Write(MessageType type, string sourceComponent, string message);
    }

    public class Logger : ILogger
    {
        private String8Block _block;
        private ITabularWriter _writer;
        public bool Failed { get; private set; }

        public Logger(IStreamProvider streamProvider, string outputFilePath)
        {
            string logFilePath = Path.Combine(outputFilePath, "Log.csv");
            _writer = TabularFactory.BuildWriter(streamProvider.OpenWrite(logFilePath), logFilePath);
            _writer.SetColumns(new string[] { "WhenUtc", "MessageType", "SourceComponent", "Message" });

            _block = new String8Block();
        }

        public void Write(MessageType type, string sourceComponent, string message)
        {
            if (type == MessageType.AssertFailed) this.Failed = true;

            _writer.Write(DateTime.UtcNow);
            _writer.Write(_block.GetCopy(type.ToString()));
            _writer.Write(_block.GetCopy(sourceComponent));
            _writer.Write(_block.GetCopy(message));

            _writer.NextRow();
            _block.Clear();
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }
    }
}
