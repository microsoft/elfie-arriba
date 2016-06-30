// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class BinaryReaderWriterExtensions
    {
        /// <summary>
        ///  Write an array of a primitive type as a single write operation.
        /// </summary>
        /// <param name="writer">BinaryWriter to write to</param>
        /// <param name="array">Array to write out</param>
        /// <param name="index">Index from which to write</param>
        /// <param name="length">Number of elements to write</param>
        public static void WritePrimitiveArray(this BinaryWriter writer, Array array, int index = 0, int length = -1)
        {
            // Default length if not provided
            if (length == -1) length = array.Length - index;

            // Validate arguments
            if (array == null) throw new ArgumentNullException("array");
            if (index < 0 || index > array.Length) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || index + length > array.Length) throw new ArgumentOutOfRangeException("length");

            // Validate type is writeable
            Type elementType = array.GetType().GetElementType();
            if (!elementType.IsPrimitive) throw new ArgumentException(String.Format(Resources.WritePrimitiveArray_NoPrimitiveType, elementType.Name));

            // Copy array to byte[] for serialization
            byte elementSize = (byte)Marshal.SizeOf(elementType);
            byte[] buffer = new byte[elementSize * length];
            Buffer.BlockCopy(array, elementSize * index, buffer, 0, elementSize * length);

            // Write element size, element count, and then bytes themselves
            writer.Write(elementSize);
            writer.Write(length);
            writer.Write(buffer, 0, elementSize * length);
        }

        /// <summary>
        ///  Read an array of a primitive type as a single read operation.
        /// </summary>
        /// <typeparam name="T">Type of array element to read</typeparam>
        /// <param name="reader">BinaryReader to read from</param>
        /// <returns>T[] from the binary stream with the number of elements the stream says were written</returns>
        public static T[] ReadPrimitiveArray<T>(this BinaryReader reader)
        {
            // Confirm element size matches
            Type elementType = typeof(T);
            byte elementSize = (byte)Marshal.SizeOf(elementType);
            byte readElementSize = reader.ReadByte();
            if (elementSize != readElementSize) throw new IOException(String.Format(Resources.ReadPrimitiveArray_WrongElementSize, elementType.Name, elementSize, readElementSize));

            // Read count and values
            int elementCount = reader.ReadArrayLength(elementSize);
            byte[] buffer = reader.ReadBytes(elementSize * elementCount);

            // Copy to array of final type
            T[] values = new T[elementCount];
            Buffer.BlockCopy(buffer, 0, values, 0, elementSize * elementCount);

            return values;
        }

        /// <summary>
        ///  Write a List/Array of IBinarySerializable things easily.
        /// </summary>
        /// <typeparam name="T">Type of items in array to write</typeparam>
        /// <param name="writer">BinaryWriter to write to</param>
        /// <param name="items">IList of items to write</param>
        /// <param name="index">Index from which to write</param>
        /// <param name="length">Number of items to write</param>
        public static void Write<T>(this BinaryWriter writer, IList<T> items, int index = 0, int length = -1) where T : IBinarySerializable
        {
            // Default length if not provided
            if (length == -1) length = items.Count - index;

            // Validate arguments
            if (items == null) throw new ArgumentNullException("items");
            if (index < 0 || index > items.Count) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || index + length > items.Count) throw new ArgumentOutOfRangeException("length");

            // Write number of items
            writer.Write(length);

            // Write items individually
            for (int i = 0; i < length; ++i)
            {
                items[index + i].WriteBinary(writer);
            }
        }

        /// <summary>
        ///  Read a generic list of BinarySerializable items from a BinaryReader
        /// </summary>
        /// <typeparam name="T">Type of item to read, must be IBinarySerializable</typeparam>
        /// <param name="reader">BinaryReader to read from</param>
        /// <returns>List&lt;T&gt; from BinaryWriter, written with BinaryWriter.Write(List&lt;T&gt;)</returns>
        public static List<T> ReadList<T>(this BinaryReader reader) where T : IBinarySerializable, new()
        {
            // Read number of items
            int length = reader.ReadArrayLength();

            // Read the items themselves
            List<T> values = new List<T>(length);

            for (int i = 0; i < length; ++i)
            {
                T value = new T();
                value.ReadBinary(reader);
                values.Add(value);
            }

            return values;
        }

        /// <summary>
        ///  Helper to read the length of an array from a BinaryReader. In addition to reading the
        ///  length, this method confirms that the remaining stream is long enough to contain the
        ///  reported number of items. This helps to ensure serializable objects fail cleanly when
        ///  reading an unexpected format.
        /// </summary>
        /// <param name="reader">BinaryReader to read from</param>
        /// <param name="elementSizeInBytes">Size of each element in bytes</param>
        /// <returns>Length value written at this point in the stream</returns>
        public static int ReadArrayLength(this BinaryReader reader, int elementSizeInBytes = 1)
        {
            long position = reader.BaseStream.Position;

            // Read Length
            int length = reader.ReadInt32();
            if (length < 0) throw new IOException(String.Format(Resources.ReadArrayLength_LengthInvalid, position, length));

            // Validate length is positive and fits in remaining stream
            long remainingLength = reader.BaseStream.Length - position - 4;
            if (length * elementSizeInBytes > remainingLength) throw new IOException(String.Format(Resources.ReadArrayLength_LengthInvalid, reader.BaseStream.Position - 4, length));

            return length;
        }
    }
}
