using System;
using System.IO;
using System.Runtime.Serialization;

namespace XForm.IO
{
    [Serializable]
    public class ColumnDataNotFoundException : IOException
    {
        public string ColumnPath { get; set; }

        public ColumnDataNotFoundException() { }
        public ColumnDataNotFoundException(string message) : base(message) { }
        public ColumnDataNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ColumnDataNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ColumnDataNotFoundException(string message, string columnPath) : base(message)
        {
            ColumnPath = columnPath;
        }
    }
}
