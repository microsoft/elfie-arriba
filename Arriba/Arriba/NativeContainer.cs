// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba
{
    public static class NativeContainer
    {
        public static TUNTYPED CreateTypedInstance<TUNTYPED>(Type genericType, Type typeParam)
        {
            Type specificType = genericType.MakeGenericType(typeParam);
            TUNTYPED untyped = (TUNTYPED)Activator.CreateInstance(specificType);
            return untyped;
        }
    }
}
