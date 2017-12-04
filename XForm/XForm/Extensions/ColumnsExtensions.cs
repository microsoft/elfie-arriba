// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using XForm.Data;
using XForm.Query;

namespace XForm.Extensions
{
    public static class ColumnsExtensions
    {
        public static int IndexOfColumn(this IReadOnlyList<ColumnDetails> columns, string columnName)
        {
            int index;
            if (TryGetIndexOfColumn(columns, columnName, out index)) return index;

            throw new ColumnNotFoundException(columnName, columns.Select((cd) => cd.Name));
        }

        public static bool TryGetIndexOfColumn(this IReadOnlyList<ColumnDetails> columns, string columnName, out int index)
        {
            index = -1;

            for (int i = 0; i < columns.Count; ++i)
            {
                if (columns[i].Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class ColumnNotFoundException : UsageException
    {
        public ColumnNotFoundException(string columnName, IEnumerable<string> validColumnNames) : base(columnName, "columnName", validColumnNames)
        { }

        public ColumnNotFoundException() { }
        public ColumnNotFoundException(string message) : base(message) { }
        public ColumnNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ColumnNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
