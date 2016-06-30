// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Model.Security
{
    public enum IdentityScope : byte
    {
        /// <summary>
        /// Identity scope of a single user.
        /// </summary>
        User = 1,

        /// <summary>
        /// Identity scope of a group of users. 
        /// </summary>
        Group = 2
    }
}
