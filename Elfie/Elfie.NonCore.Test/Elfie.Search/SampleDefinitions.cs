// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Indexer;

namespace Microsoft.CodeAnalysis.Elfie.Test.Elfie.Search
{
    /// <summary>
    ///  A sample class for RoslynDefinitionFinder to identify definitions from.
    /// </summary>
    public class SampleDefinitions
    {
        private string _cachePath = "";

        private static RsDsSignature GetAssemblyDebugSignature(string binaryFilePath)
        {
            using (var stream = new FileStream(binaryFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Assembly.ReadRsDsSignature(stream);
            }
        }

        public string GetSymbolCachePdbPath(string binaryFilePath)
        {
            RsDsSignature signature = Assembly.ReadRsDsSignature(binaryFilePath);
            if (signature == null) return null;

            string pdbFileName = Path.GetFileNameWithoutExtension(binaryFilePath) + ".pdb";
            return Path.Combine(_cachePath, pdbFileName, signature.ToString(), signature.Guid.ToString(), pdbFileName);
        }

        public void SetDefaultCachePath()
        {
            SetCachePath(Environment.ExpandEnvironmentVariables(@"%SystemDrive%\SymbolCache"));
        }

        public void SetCachePath(string value)
        {
            _cachePath = value;
        }
    }
}
