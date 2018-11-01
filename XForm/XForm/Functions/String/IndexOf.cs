// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class IndexOfBuilder : IFunctionBuilder
    {
        public string Name => "IndexOf";
        public string Usage => "IndexOf({String8}, {Value})";
        public Type ReturnType => typeof(int);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn text = context.Parser.NextColumn(source, context, typeof(String8));
            string value = context.Parser.NextString();
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);

            return SimpleTransformFunction<String8, int>.Build(
                Name,
                source,
                text,
                (string8) => string8.IndexOf(value8));
        }
    }
}
