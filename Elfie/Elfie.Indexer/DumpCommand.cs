// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.Elfie.Indexer
{
    public class DumpCommand
    {
        public int Run(DumpOptions dumpOptions)
        {
            foreach (PackageDatabase db in BinarySerializableExtensions.LoadEach<PackageDatabase>(dumpOptions.IndexFilePath, true))
            {
                Console.Write(db.ToString());
            }
            return 0;
        }
    }
}
