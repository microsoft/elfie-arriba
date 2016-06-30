// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class IntExtensions
    {
        /// <summary>
        ///  Convert an integer value to a ushort, returning ushort.MaxValue if
        ///  the integer was too big.
        /// </summary>
        /// <param name="value">Integer value to convert</param>
        /// <returns>ushort of value, 0 if value was negative, ushort.MaxValue if value was over ushort.MaxValue</returns>
        public static ushort TrimToUShort(this int value)
        {
            if (value < 0) return 0;
            if (value > ushort.MaxValue) return ushort.MaxValue;
            return (ushort)value;
        }
    }
}
