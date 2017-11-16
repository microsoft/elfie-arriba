// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Data
{
    public static class Allocator
    {
        public static void AllocateToSize<T>(ref T[] array, int minimumSize)
        {
            if (array == null || array.Length < minimumSize) array = new T[minimumSize];
        }
    }
}
