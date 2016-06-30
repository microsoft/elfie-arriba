// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Structures;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  SelectResult is the object returned by a select query. It contains
    ///  the total count matching the query, the number of items returned,
    ///  and the specific columns and values for the returned rows.
    /// </summary>
    public class SelectResult : BaseResult
    {
        public uint Total { get; set; }
        public ushort CountReturned { get; set; }
        public SelectQuery Query { get; set; }
        public DataBlock Values { get; set; }

        public SelectResult(SelectQuery query) : base()
        {
            this.Query = query;
        }
    }
}
