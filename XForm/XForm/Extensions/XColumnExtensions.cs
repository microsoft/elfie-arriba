// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using XForm.Data;
using XForm.IO;
using XForm.Query;

namespace XForm.Extensions
{
    public static class XColumnExtensions
    {
        public static bool IsEnumColumn(this IXColumn column)
        {
            return column.IndicesType != null;
        }

        public static bool IsConstantColumn(this IXColumn column)
        {
            return (column.IndicesType == null && column.ValuesGetter() != null);
        }

        public static bool IsNullConstant(this IXColumn column)
        {
            if (!column.IsConstantColumn()) return false;

            XArray value = column.ValuesGetter()();
            return (value.HasNulls && value.NullRows[value.Index(0)]);
        }

        public static IXColumn Find(this IReadOnlyList<IXColumn> columns, string columnName)
        {
            return columns[columns.IndexOfColumn(columnName)];
        }

        public static bool TryFind(this IReadOnlyList<IXColumn> columns, string columnName, out IXColumn column)
        {
            int index;
            if (columns.TryGetIndexOfColumn(columnName, out index))
            {
                column = columns[index];
                return true;
            }

            column = null;
            return false;
        }

        public static int IndexOfColumn(this IReadOnlyList<IXColumn> columns, string columnName)
        {
            int index;
            if (TryGetIndexOfColumn(columns, columnName, out index)) return index;

            throw new ColumnNotFoundException(columnName, columns.Select((cd) => cd.ColumnDetails.Name));
        }

        public static bool TryGetIndexOfColumn(this IReadOnlyList<IXColumn> columns, string columnName, out int index)
        {
            index = -1;

            for (int i = 0; i < columns.Count; ++i)
            {
                if (columns[i].ColumnDetails.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
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
