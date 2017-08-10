using System;
using System.IO;
using V5;
using V5.Collections;

namespace V5.Data
{
    public class PrimitiveColumn<T> where T : IComparable<T>
    {
        public string Name { get; private set; }
        public T[] Values { get; private set; }
        public int Count { get; private set; }

        public PrimitiveColumn(string name, T[] values, int count = -1)
        {
            this.Name = name;
            this.Values = values;
            this.Count = (count < 0 ? values.Length : count);
        }

        public void Where(IndexSet set, BooleanOperator bOp, CompareOperator op, T value)
        {
            set.Where(bOp, this.Values, op, value);
        }

        public void Where(ref Span<int> page, BooleanOperator bOp, CompareOperator op, T value, int offset = 0)
        {
            if (bOp != BooleanOperator.And) throw new NotImplementedException();
            if (op != CompareOperator.GreaterThan) throw new NotImplementedException();

            int nextWriteIndex = 0;
            for (int i = 0; i < page.Length; ++i)
            {
                if (this.Values[i + offset].CompareTo(value) > 0)
                {
                    page[nextWriteIndex++] = i;
                }
            }
        }

        public static PrimitiveColumn<T> Read(string partitionPath, string columnName)
        {
            string columnValuesPath = Path.Combine(partitionPath, columnName, "V");
            return new PrimitiveColumn<T>(columnName, BinarySerializer.Read<T>(columnValuesPath));
        }

        public void Write(string partitionPath)
        {
            string columnValuesPath = Path.Combine(partitionPath, this.Name, "V");
            BinarySerializer.Write(columnValuesPath, this.Values);
        }
    }
}
