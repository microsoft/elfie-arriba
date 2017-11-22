using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.IO;

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

    public class Logger : IDisposable
    {
        private String8Block _block;
        private ITabularWriter _writer;

        public Logger(string outputFilePath)
        {
            string logFilePath = Path.Combine(outputFilePath, "Log.csv");
            _writer = TabularFactory.BuildWriter(logFilePath);
            _writer.SetColumns(new string[] { "WhenUtc", "MessageType", "SourceComponent", "Message" });

            _block = new String8Block();
        }

        public void Write(MessageType type, string sourceComponent, string message)
        {
            _writer.Write(DateTime.UtcNow);
            _writer.Write(_block.GetCopy(type.ToString()));
            _writer.Write(_block.GetCopy(sourceComponent));
            _writer.Write(_block.GetCopy(message));

            _writer.NextRow();
            _block.Clear();
        }

        public void Dispose()
        {
            if(_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }
    }
}
