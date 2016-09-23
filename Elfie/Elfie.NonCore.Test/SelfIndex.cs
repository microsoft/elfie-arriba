// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.Elfie.Test
{
    public class SelfIndex
    {
#if DEBUG
        public const string BinaryPathToIndex = @"..\..\..\bin\Debug\Microsoft.CodeAnalysis.Elfie.dll";
#else
        public const string BinaryPathToIndex = @"..\..\..\bin\Release\Microsoft.CodeAnalysis.Elfie.dll";
#endif

        public const string SampleIndexPath = @"..\..\..\Microsoft.CodeAnalysis.Elfie.dll.idx";
        public const string TextModelPath = @"..\..\..\Microsoft.CodeAnalysis.Elfie.dll.idx.log";
        public const string StringStorePath = @"..\..\..\Microsoft.CodeAnalysis.Elfie.dll.idx.str";

        public static void EnsureSampleIndexBuilt()
        {
            if (!File.Exists(TextModelPath))
            {
                PackageDatabase db = Indexer.IndexCommand.Index(BinaryPathToIndex, true);
                Indexer.IndexCommand.Save(db, Path.GetDirectoryName(TextModelPath));
                Indexer.IndexCommand.SaveDiagnostics(db, Path.GetDirectoryName(TextModelPath));
            }
        }
    }
}
