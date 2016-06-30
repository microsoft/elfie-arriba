// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Structures;

namespace Arriba.Model.Aggregations
{
    /// <summary>
    ///  CountAggregator computes the COUNT of a query, ignoring any columns.
    /// </summary>
    public class CountAggregator : IAggregator
    {
        public override string ToString()
        {
            return "COUNT";
        }

        public object CreateContext()
        {
            return null;
        }

        public bool RequireMerge
        {
            get { return false; }
        }

        public object Aggregate(object context, ShortSet matches, IUntypedColumn[] columns)
        {
            if (matches == null) throw new ArgumentNullException("matches");
            return (ulong)matches.Count();
        }

        public object Merge(object context, object[] values)
        {
            if (values == null) throw new ArgumentNullException("values");

            bool allAreNull = true;
            ulong result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                if (values[i] != null)
                {
                    allAreNull = false;
                    result += (ulong)values[i];
                }
            }

            if (allAreNull) return null;
            return result;
        }
    }
}
