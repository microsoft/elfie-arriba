// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Arriba.Model.Security
{
    public enum PermissionScope : byte
    {
        /// <summary>
        /// User can read data
        /// </summary>
        Reader,

        /// <summary>
        /// User can read and write data
        /// </summary>
        Writer,

        /// <summary>
        /// User can read and write data. User can modify permissions and metadata.
        /// </summary>
        Owner
    }
}
