// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text;

namespace Arriba.Diagnostics
{
    /// <summary>
    ///  TraceWriter is a simple TextWriter implementation which forwards output to Trace output.
    ///  It is used as a default output destination for the other console classes.
    /// </summary>
    public class TraceWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public override void Write(char value)
        {
            Trace.Write(value);
        }

        public override void Write(char[] value)
        {
            Trace.Write(new string(value));
        }

        public override void Write(string value)
        {
            Trace.Write(value);
        }

        public override void WriteLine(char value)
        {
            Trace.WriteLine(value);
        }

        public override void WriteLine(char[] value)
        {
            Trace.WriteLine(new string(value));
        }

        public override void WriteLine(string value)
        {
            Trace.WriteLine(value);
        }
    }
}
