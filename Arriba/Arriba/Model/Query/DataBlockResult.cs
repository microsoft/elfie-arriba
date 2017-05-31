// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  Base result type for results which include a DataBlock.
    ///  Allows uniform access to the results for post-processing,
    ///  like column security.
    /// </summary>
    public class DataBlockResult : BaseResult
    {
        public IQuery Query { get; set; }
        public long Total { get; set; }
        public DataBlock Values { get; set; }

        public DataBlockResult(IQuery query)
        {
            this.Query = query;
        }
    }
}
