using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using XForm.Data;

namespace XForm.Extensions
{
    public static class IReadOnlyListExtensions
    {
        public static int IndexOfColumn(this IReadOnlyList<ColumnDetails> columns, string columnName)
        {
            for(int index = 0; index < columns.Count; ++index)
            {
                if (columns[index].Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase)) return index;
            }

            throw new ColumnNotFoundException($"Column \"{columnName}\" not found in source. Columns: \"{String.Join(", ", columns.Select((cd) => cd.Name))}.");
        }
    }

    [Serializable]
    public class ColumnNotFoundException : Exception
    {
        public ColumnNotFoundException() { }
        public ColumnNotFoundException(string message) : base(message) { }
        public ColumnNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ColumnNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
