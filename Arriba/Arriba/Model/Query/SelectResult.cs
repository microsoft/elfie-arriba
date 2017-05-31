// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Structures;

namespace Arriba.Model.Query
{
    public class SelectResult : DataBlockResult
    {
        public ushort CountReturned { get; set; }

        internal DataBlock OrderByValues { get; set; }

        public SelectResult(SelectQuery query) : base(query) { }
    }
}
