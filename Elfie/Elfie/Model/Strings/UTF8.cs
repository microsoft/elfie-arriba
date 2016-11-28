﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    internal class UTF8
    {
        public const byte Null = (byte)'\0';
        public const byte Quote = (byte)'"';
        public const byte Comma = (byte)',';
        public const byte Period = (byte)'.';
        public const byte Backslash = (byte)'\\';
        public const byte Pipe = (byte)'|';
        public const byte Tab = (byte)'\t';
        public const byte CR = (byte)'\r';
        public const byte LF = (byte)'\n';
        public const byte Newline = (byte)'\n';
        public const byte Space = (byte)' ';
        public const byte a = (byte)'a';
        public const byte z = (byte)'z';
        public const byte A = (byte)'A';
        public const byte Z = (byte)'Z';
        public const byte Zero = (byte)'0';
        public const byte Nine = (byte)'9';

        public const byte ToUpperSubtract = 'a' - 'A';
        public const byte AlphabetLength = 'z' - 'a' + 1;
        public const byte DigitsLength = '9' - '0' + 1;
    }
}
