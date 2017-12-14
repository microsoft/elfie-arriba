// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Transforms;

namespace XForm.Types
{
    public class PrimitiveTypeProvider<T> : ITypeProvider where T : IComparable<T>
    {
        public string Name => typeof(T).Name;

        public Type Type => typeof(T);

        public PrimitiveTypeProvider()
        {
            if (!typeof(T).IsPrimitive) throw new ArgumentException($"PrimitiveTypeProvider does not support non-primitive type {typeof(T).Name}");
        }

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath)
        {
            return new PrimitiveArrayReader<T>(streamProvider.OpenRead(ValuesFilePath(columnPath)));
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new PrimitiveArrayWriter<T>(streamProvider.OpenWrite(ValuesFilePath(columnPath)));
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // TODO: Add primitive number conversions
            return null;
        }

        public Action<DataBatch, DataBatch, RowRemapper> TryGetComparer(CompareOperator op)
        {
            if (typeof(T) == typeof(int)) return new IntComparer().TryBuild(op);
            return new ComparableComparer<T>().TryBuild(op);
        }

        public static string ValuesFilePath(string columnPath)
        {
            return Path.Combine(columnPath, $"V.{BinaryFileTypePart()}.bin");
        }

        public static string BinaryFileTypePart()
        {
            if (typeof(T) == typeof(bool)) return "b8";

            if (typeof(T) == typeof(sbyte)) return "i8";
            if (typeof(T) == typeof(short)) return "i16";
            if (typeof(T) == typeof(ushort)) return "u16";
            if (typeof(T) == typeof(int)) return "i32";
            if (typeof(T) == typeof(uint)) return "u32";
            if (typeof(T) == typeof(long)) return "i64";
            if (typeof(T) == typeof(ulong)) return "u64";
            if (typeof(T) == typeof(float)) return "f32";
            if (typeof(T) == typeof(double)) return "f64";

            throw new ArgumentException($"PrimitiveTypeProvider doesn't know how to read type {typeof(T).Name}.");
        }
    }

    public class PrimitiveArrayReader<T> : IColumnReader
    {
        private const int ReadPageSize = 64 * 1024;

        private int _bytesPerItem;
        private ByteReader _byteReader;
        private T[] _array;

        public PrimitiveArrayReader(Stream stream)
        {
            _byteReader = new ByteReader(stream);
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public int Count => (int)(_byteReader.Count / _bytesPerItem);

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            // Allocate the result array
            Allocator.AllocateToSize(ref _array, selector.Count);

            // Read items in pages of 64k
            int byteStart = _bytesPerItem * selector.StartIndexInclusive;
            int byteEnd = _bytesPerItem * selector.EndIndexExclusive;
            int bytesRead = 0;
            for (int currentByteIndex = byteStart; currentByteIndex < byteEnd; currentByteIndex += ReadPageSize)
            {
                int currentByteEnd = Math.Min(byteEnd, currentByteIndex + ReadPageSize);
                DataBatch byteBatch = _byteReader.Read(ArraySelector.All(int.MaxValue).Slice(currentByteIndex, currentByteEnd));
                Buffer.BlockCopy(byteBatch.Array, 0, _array, bytesRead, byteBatch.Count);
                bytesRead += currentByteEnd - currentByteIndex;
            }

            return DataBatch.All(_array, selector.Count);
        }

        public void Dispose()
        {
            if (_byteReader != null)
            {
                _byteReader.Dispose();
                _byteReader = null;
            }
        }
    }

    public class PrimitiveArrayWriter<T> : IColumnWriter
    {
        private int _bytesPerItem;
        private Stream _stream;
        private byte[] _bytesBuffer;

        public PrimitiveArrayWriter(Stream stream)
        {
            _stream = stream;
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public void Append(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _bytesBuffer, _bytesPerItem * batch.Count);

            if (batch.Selector.Indices == null && batch.Selector.IsSingleValue == false)
            {
                Buffer.BlockCopy(batch.Array, _bytesPerItem * batch.Selector.StartIndexInclusive, _bytesBuffer, 0, _bytesPerItem * batch.Count);
            }
            else
            {
                for (int i = 0; i < batch.Count; ++i)
                {
                    int index = batch.Index(i);
                    Buffer.BlockCopy(batch.Array, _bytesPerItem * index, _bytesBuffer, i * _bytesPerItem, _bytesPerItem);
                }
            }

            _stream.Write(_bytesBuffer, 0, _bytesPerItem * batch.Count);
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }

    internal class IntComparer : IDataBatchComparer
    {
        // IntComparer demonstrates the performance possible if faster-to-evaluate cases are implemented specially per type.
        public void WhereEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            int[] leftArray = (int[])left.Array;
            int[] rightArray = (int[])right.Array;

            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] == rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] == rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] == rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                int rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] == rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] == rightArray[right.Selector.StartIndexInclusive])
                {
                    for (int i = 0; i < left.Count; ++i) result.Add(i);
                }
            }
        }

        public void WhereNotEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            int[] leftArray = (int[])left.Array;
            int[] rightArray = (int[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                int leftIndex = left.Index(i);
                int rightIndex = right.Index(i);
                if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                if (leftArray[leftIndex] != rightArray[rightIndex]) result.Add(i);
            }
        }

        public void WhereLessThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            int[] leftArray = (int[])left.Array;
            int[] rightArray = (int[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                int leftIndex = left.Index(i);
                int rightIndex = right.Index(i);
                if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                if (leftArray[leftIndex] < rightArray[rightIndex]) result.Add(i);
            }
        }

        public void WhereLessThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            int[] leftArray = (int[])left.Array;
            int[] rightArray = (int[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                int leftIndex = left.Index(i);
                int rightIndex = right.Index(i);
                if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                if (leftArray[leftIndex] <= rightArray[rightIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            int[] leftArray = (int[])left.Array;
            int[] rightArray = (int[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                int leftIndex = left.Index(i);
                int rightIndex = right.Index(i);
                if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                if (leftArray[leftIndex] > rightArray[rightIndex]) result.Add(i);
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            int[] leftArray = (int[])left.Array;
            int[] rightArray = (int[])right.Array;

            for (int i = 0; i < left.Count; ++i)
            {
                int leftIndex = left.Index(i);
                int rightIndex = right.Index(i);
                if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                if (leftArray[leftIndex] >= rightArray[rightIndex]) result.Add(i);
            }
        }
    }
}
