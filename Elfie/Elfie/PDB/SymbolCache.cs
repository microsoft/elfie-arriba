// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Configuration;

namespace Microsoft.CodeAnalysis.Elfie.PDB
{
    public static class SymbolCache
    {
        public static string Path { get; set; }

        static SymbolCache()
        {
            Path = ConfigurationManager.AppSettings["SymbolCachePath"];
            if (String.IsNullOrEmpty(Path)) Path = @"%SystemDrive%\SymbolCache";

            Path = Environment.ExpandEnvironmentVariables(Path);
        }
    }
}
