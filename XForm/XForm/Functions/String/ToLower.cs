// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class ToLowerBuilder : IFunctionBuilder
    {
        public string Name => "ToLower";
        public string Usage => "ToLower({String8})";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            // Create a String8Block to hold the Lowercase copy of the values
            String8Block block = new String8Block();

            return SimpleTransformFunction<String8, String8>.Build(
                Name,
                source,
                context.Parser.NextColumn(source, context, typeof(String8)),
                (string8) =>
                {
                    // Make a copy, make it Lowercase, and return it
                    String8 Lower = block.GetCopy(string8);
                    Lower.ToLowerInvariant();
                    return Lower;
                },
                () =>
                {
                    // Before each page, clear the String8Block to reuse the memory
                    block.Clear();
                }
            );
        }
    }
}
