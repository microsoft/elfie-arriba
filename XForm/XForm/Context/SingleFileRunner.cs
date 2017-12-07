// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.IO;

namespace XForm.Context
{
    /// <summary>
    ///  SingleFileRunner has no Database model and only will return a reader for raw files or
    ///  existing binary tables.
    /// </summary>
    public class SingleFileRunner : IWorkflowRunner
    {
        public IEnumerable<string> SourceNames => Array.Empty<string>();

        public IDataBatchEnumerator Build(string sourceName, WorkflowContext context)
        {
            if (sourceName.StartsWith("Table\\", StringComparison.OrdinalIgnoreCase) || sourceName.EndsWith(".xform", StringComparison.OrdinalIgnoreCase))
            {
                return new BinaryTableReader(context.StreamProvider, sourceName);
            }
            else
            {
                return new TabularFileReader(context.StreamProvider, sourceName);
            }
        }
    }
}
