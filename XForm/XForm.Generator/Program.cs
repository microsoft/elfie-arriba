// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace XForm.Generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string xformProjectPath = Path.GetFullPath(Path.Combine(@"..\..\..\XForm"));

            string comparerPath = Path.Combine(xformProjectPath, @"Types\Comparers");
            Console.WriteLine($"Building Comparers to [{comparerPath}]...");
            ComparerGenerator.GenerateAll(comparerPath);

            string primitiveConverterPath = Path.Combine(xformProjectPath, @"Types\PrimitiveConverters.cs");
            Console.WriteLine($"Building [{primitiveConverterPath}]...");
            PrimitiveConvertersGenerator.Generate(primitiveConverterPath);

            Console.WriteLine("Done.");
        }
    }
}
