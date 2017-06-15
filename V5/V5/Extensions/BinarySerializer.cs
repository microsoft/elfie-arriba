// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace V5
{
    public static class BinarySerializer
    {
        private static List<Tuple<Type, string>> TypeIdentifierMap;

        static BinarySerializer()
        {
            TypeIdentifierMap = new List<Tuple<Type, string>>();

            TypeIdentifierMap.Add(Tuple.Create(typeof(bool), "b1"));
            TypeIdentifierMap.Add(Tuple.Create(typeof(byte), "b8"));

            TypeIdentifierMap.Add(Tuple.Create(typeof(short), "i16"));
            TypeIdentifierMap.Add(Tuple.Create(typeof(int), "i32"));
            TypeIdentifierMap.Add(Tuple.Create(typeof(long), "i64"));

            TypeIdentifierMap.Add(Tuple.Create(typeof(ushort), "u16"));
            TypeIdentifierMap.Add(Tuple.Create(typeof(uint), "u32"));
            TypeIdentifierMap.Add(Tuple.Create(typeof(ulong), "u64"));

            TypeIdentifierMap.Add(Tuple.Create(typeof(float), "f32"));
            TypeIdentifierMap.Add(Tuple.Create(typeof(double), "f64"));
        }

        public static string TypeIdentifier<T>()
        {
            Type type = typeof(T);
            Tuple<Type, string> match = TypeIdentifierMap.FirstOrDefault((t) => t.Item1 == type);
            if(match != null) return match.Item2;
            return null;
        }

        public static Type Type(string identifier)
        {
            Tuple<Type, string> match = TypeIdentifierMap.FirstOrDefault((t) => t.Item2.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match.Item1;
            return null;
        }

        public static void Write<T>(string filePath, T[] array)
        {
            string fullPath = $"{filePath}.{TypeIdentifier<T>()}.bin";
            string temporaryPath = Path.ChangeExtension(fullPath, ".new");

            // Ensure the containing folder exists
            string serializationDirectory = Path.GetDirectoryName(fullPath);
            if (!String.IsNullOrEmpty(serializationDirectory)) Directory.CreateDirectory(serializationDirectory);

            // Serialize the item
            long lengthWritten = 0;
            FileStream s = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.Delete);
            using (BinaryWriter writer = new BinaryWriter(s))
            {
                writer.Write(array);
                lengthWritten = s.Position;
            }

            if (lengthWritten == 0)
            {
                // If nothing was written, delete the file
                File.Delete(temporaryPath);
                File.Delete(fullPath);
            }
            else
            {
                // Otherwise, replace the previous official file with the new one
                File.Move(temporaryPath, fullPath);
            }
        }

        public static T[] Read<T>(string filePath)
        {
            string fullPath = $"{filePath}.{TypeIdentifier<T>()}.bin";

            FileStream s = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (BinaryReader reader = new BinaryReader(s))
            {
                return reader.ReadArray<T>(s.Length);
            }
        }
    }

    public static class BinaryReaderWriterExtensions
    {
        /// <summary>
        ///  Write an array of a primitive type as a single write operation.
        /// </summary>
        /// <param name="writer">BinaryWriter to write to</param>
        /// <param name="array">Array to write out</param>
        /// <param name="index">Index from which to write</param>
        /// <param name="length">Number of elements to write</param>
        public static void Write<T>(this BinaryWriter writer, T[] array, int index = 0, int length = -1)
        {
            // Default length if not provided
            if (length == -1) length = array.Length - index;

            // Validate arguments
            if (array == null) throw new ArgumentNullException("array");
            if (index < 0 || index > array.Length) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || index + length > array.Length) throw new ArgumentOutOfRangeException("length");

            // Copy array to byte[] for serialization
            int elementSize = Marshal.SizeOf<T>();
            byte[] buffer = new byte[elementSize * length];
            Buffer.BlockCopy(array, elementSize * index, buffer, 0, elementSize * length);

            // Write the bytes
            writer.Write(buffer);
        }

        /// <summary>
        ///  Read an array of a primitive type as a single read operation.
        /// </summary>
        /// <typeparam name="T">Type of array element to read</typeparam>
        /// <param name="reader">BinaryReader to read from</param>
        /// <returns>T[] from the binary stream with the number of elements the stream says were written</returns>
        public static T[] ReadArray<T>(this BinaryReader reader, long lengthBytes)
        {
            int elementSize = Marshal.SizeOf<T>();
            long arrayLength = lengthBytes / elementSize;

            // Read raw bytes
            byte[] buffer = reader.ReadBytes((int)lengthBytes);

            // Return byte[] directly
            if (typeof(T) == typeof(byte)) return (T[])(Array)buffer;

            // Copy to array of final type
            T[] values = new T[arrayLength];
            Buffer.BlockCopy(buffer, 0, values, 0, (int)lengthBytes);

            return values;
        }

        public static long LengthBytes<T>(this T[] array)
        {
            return array.Length * Marshal.SizeOf<T>();
        }
    }
}
