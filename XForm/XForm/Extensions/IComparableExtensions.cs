// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Extensions
{
    public static class IComparableExtensions
    {
        public static T BiggestOf<T>(this T left, T right) where T : IComparable<T>
        {
            return left.CompareTo(right) > 0 ? left : right;
        }
    }
}
