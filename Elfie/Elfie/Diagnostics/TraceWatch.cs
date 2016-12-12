// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

using Microsoft.CodeAnalysis.Elfie.Extensions;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  TraceWatch is used to log about an activity that takes time to complete
    ///  using the IDisposable pattern. Wrap the activity in a using block and this
    ///  will log the total runtime in a human-friendly form.
    /// </summary>
    public sealed class TraceWatch : IDisposable
    {
        private Stopwatch Watch { get; set; }

        public TraceWatch(string message)
        {
            this.Watch = Stopwatch.StartNew();
            if (!String.IsNullOrEmpty(message)) Trace.WriteLine(message);
        }

        public TraceWatch(string format, params object[] arguments) : this(String.Format(CultureInfo.InvariantCulture, format, arguments))
        { }

        public void Dispose()
        {
            this.Watch.Stop();
            Trace.WriteLine(String.Format(CultureInfo.InvariantCulture, " -> {0}", this.Watch.Elapsed.ToFriendlyString()));
        }
    }
}
