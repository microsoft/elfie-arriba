// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Arriba.Serialization
{
    /// <summary>
    ///  IBinarySerializable should be implemented to provide completely custom
    ///  binary serialization. An empty constructor must also be provided for the
    ///  class.
    /// </summary>
    public interface IBinarySerializable
    {
        /// <summary>
        ///  Method to initialize class members by reading from the source context.
        /// </summary>
        /// <param name="context">ISerializationContext to use to load state</param>
        void ReadBinary(ISerializationContext context);

        /// <summary>
        ///  Method to write class members out to the destination context.
        /// </summary>
        /// <param name="context">ISerializationContext to use to write state</param>
        void WriteBinary(ISerializationContext context);
    }
}
