// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Query
{
    /// <summary>
    ///  IUsage is implemented by builders which describe how they should be used if arguments are missing.
    /// </summary>
    public interface IUsage
    {
        /// <summary>
        ///  Usage string to write out if this component wasn't passed the right arguments.
        /// </summary>
        string Usage { get; }
    }
}
