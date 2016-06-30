// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using Arriba.Extensions;

namespace Arriba.Serialization
{
    public static class BinaryBlockSerializer
    {
        private const string UnsupportedTypeFormatString = "BinaryBlockSerializer does not support serializing type '{0}'";
        private const byte ArrayTerminator = 0xCC;
        private const byte VariableSizeMarker = 0xAA;

        #region Read, Write
        public static T Read<T>(ISerializationContext context)
        {
            if (typeof(T) == typeof(byte[]))
            {
                return (T)(object)ReadByteArray(context);
            }
            else if (typeof(T).IsPrimitive)
            {
                return ReadPrimitive<T>(context);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                return (T)(object)ReadDateTime(context);
            }
            else if (typeof(T) == typeof(Guid))
            {
                return (T)(object)ReadGuid(context);
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                return (T)(object)ReadTimeSpan(context);
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)ReadString(context);
            }

            throw new IOException(StringExtensions.Format(UnsupportedTypeFormatString, typeof(T).Name));
        }

        public static int Write<T>(ISerializationContext context, T value)
        {
            if (value is byte[])
            {
                byte[] array = (byte[])(object)value;
                return WriteByteArray(context, array, 0, array.Length);
            }
            else if (typeof(T).IsPrimitive)
            {
                return WritePrimitive(context, value);
            }
            else if (value is DateTime)
            {
                return WriteDateTime(context, (DateTime)(object)value);
            }
            else if (value is Guid)
            {
                return WriteGuid(context, (Guid)(object)value);
            }
            else if (value is TimeSpan)
            {
                return WriteTimeSpan(context, (TimeSpan)(object)value);
            }
            else if (value is string)
            {
                return WriteString(context, (string)(object)value);
            }

            throw new IOException(StringExtensions.Format(UnsupportedTypeFormatString, typeof(T).Name));
        }
        #endregion

        #region ReadArray, WriteArray
        public static T[] ReadArray<T>(ISerializationContext context)
        {
            return (T[])ReadArray(context, typeof(T));
        }

        public static Array ReadArray(ISerializationContext context, Type t)
        {
            if (t == typeof(byte))
            {
                return (Array)ReadByteArray(context);
            }
            else if (t.IsPrimitive)
            {
                return ReadPrimitiveArray(context, t);
            }
            else if (t == typeof(DateTime))
            {
                return (Array)ReadDateTimeArray(context);
            }
            else if (t == typeof(Guid))
            {
                return (Array)ReadGuidArray(context);
            }
            else if (t == typeof(TimeSpan))
            {
                return (Array)ReadTimeSpanArray(context);
            }
            else if (t == typeof(string))
            {
                return (Array)ReadStringArray(context);
            }

            throw new IOException(StringExtensions.Format(UnsupportedTypeFormatString, t.Name));
        }

        public static int WriteArray<T>(ISerializationContext context, T[] array)
        {
            return WriteArray(context, array, 0, array.Length);
        }

        public static int WriteArray<T>(ISerializationContext context, T[] array, int index, int length)
        {
            return WriteArray(context, typeof(T), array, index, length);
        }

        public static int WriteArray(ISerializationContext context, Type t, object array, int index, int length)
        {
            if (array is byte[])
            {
                return WriteByteArray(context, (byte[])(object)array, index, length);
            }
            else if (t.IsPrimitive)
            {
                return WritePrimitiveArray(context, t, (Array)array, index, length);
            }
            else if (t == typeof(DateTime))
            {
                return WriteDateTimeArray(context, (DateTime[])(object)array, index, length);
            }
            else if (t == typeof(Guid))
            {
                return WriteGuidArray(context, (Guid[])(object)array, index, length);
            }
            else if (t == typeof(TimeSpan))
            {
                return WriteTimeSpanArray(context, (TimeSpan[])(object)array, index, length);
            }
            else if (t == typeof(string))
            {
                return WriteStringArray(context, (string[])(object)array, index, length);
            }

            throw new IOException(StringExtensions.Format(UnsupportedTypeFormatString, t.Name));
        }
        #endregion

        #region ReadSerializableArray, WriteSerializableArray
        public static int WriteSerializableArray<T>(ISerializationContext context, T[] array, int index, int length) where T : IBinarySerializable, new()
        {
            long start = context.Stream.Position;

            context.Writer.Write(VariableSizeMarker);
            context.Writer.Write(length);

            int end = index + length;
            for (int i = index; i < end; ++i)
            {
                array[i].WriteBinary(context);
            }

            context.Writer.Write(ArrayTerminator);
            return (int)(context.Stream.Position - start);
        }

        public static T[] ReadSerializableArray<T>(ISerializationContext context) where T : IBinarySerializable, new()
        {
            ReadAndVerifyElementSize(context, VariableSizeMarker);
            int length = context.Reader.ReadInt32();

            T[] values = new T[length];
            for (int i = 0; i < length; ++i)
            {
                values[i] = new T();
                values[i].ReadBinary(context);
            }

            ReadAndVerifyTerminator(context);
            return values;
        }
        #endregion

        #region Primitives
        private static int WritePrimitiveArray(ISerializationContext context, Type t, Array array, int index, int length)
        {
            byte elementSize = (byte)Marshal.SizeOf(t);

            // Copy the value type array into the byte buffer. Buffer.BlockCopy always operates in byte size counts
            // and is essentially a memcpy between two addresses. 
            byte[] byteBuffer = new byte[elementSize * length];
            Buffer.BlockCopy(array, index, byteBuffer, 0, byteBuffer.Length);

            context.Writer.Write(elementSize);
            context.Writer.Write(length);
            context.Writer.Write(byteBuffer);
            context.Writer.Write(ArrayTerminator);

            return sizeof(byte) + sizeof(int) + byteBuffer.Length + sizeof(byte);
        }

        private static Array ReadPrimitiveArray(ISerializationContext context, Type t)
        {
            // Verify the element size
            int elementSize = Marshal.SizeOf(t);
            ReadAndVerifyElementSize(context, (byte)elementSize);

            // Calculate the buffer size and read it.
            int arrayLength = context.Reader.ReadInt32();
            int bufferSize = elementSize * arrayLength;
            byte[] byteBuffer = context.Reader.ReadBytes(bufferSize);

            // Verify the terminator
            ReadAndVerifyTerminator(context);

            // Copy the byte buffer into the value type array. Buffer.BlockCopy always operates in byte size counts
            // and is essentially a memcpy between two addresses.
            Array content = CollectionFactory.BuildArray(t, arrayLength);
            Buffer.BlockCopy(byteBuffer, 0, content, 0, bufferSize);

            return content;
        }

        private static T[] ReadPrimitiveArray<T>(ISerializationContext context)
        {
            // Verify the element size
            int elementSize = Marshal.SizeOf(typeof(T));
            ReadAndVerifyElementSize(context, (byte)elementSize);

            // Calculate the buffer size and read it.
            int arrayLength = context.Reader.ReadInt32();
            int bufferSize = elementSize * arrayLength;
            byte[] byteBuffer = context.Reader.ReadBytes(bufferSize);

            // Verify the terminator
            ReadAndVerifyTerminator(context);

            // Copy the byte buffer into the value type array. Buffer.BlockCopy always operates in byte size counts
            // and is essentially a memcpy between two addresses. 
            T[] content = new T[arrayLength];
            Buffer.BlockCopy(byteBuffer, 0, content, 0, bufferSize);

            return content;
        }

        private static int WritePrimitive<T>(ISerializationContext context, T value)
        {
            // TODO: There must be a better way; not sure how to do Buffer.BlockCopy for a single value
            return WritePrimitiveArray(context, typeof(T), new T[] { value }, 0, 1);
        }

        private static T ReadPrimitive<T>(ISerializationContext context)
        {
            T[] array = ReadPrimitiveArray<T>(context);
            return array[0];
        }
        #endregion

        #region byte[]
        public static int WriteByteArray(ISerializationContext context, byte[] array, int index, int length)
        {
            context.Writer.Write((byte)1);
            context.Writer.Write(length);
            context.Writer.Write(array, index, length);
            context.Writer.Write(ArrayTerminator);
            return sizeof(byte) + sizeof(int) + length + sizeof(byte);
        }

        public static byte[] ReadByteArray(ISerializationContext context)
        {
            ReadAndVerifyElementSize(context, 1);
            int length = context.Reader.ReadInt32();
            byte[] values = context.Reader.ReadBytes(length);
            ReadAndVerifyTerminator(context);
            return values;
        }
        #endregion

        #region DateTime
        public static int WriteDateTimeArray(ISerializationContext context, DateTime[] array, int index, int length)
        {
            context.Writer.Write((byte)8);
            context.Writer.Write(length);

            int end = index + length;
            for (int i = index; i < end; ++i)
            {
                context.Writer.Write(array[i].ToUniversalTime().Ticks);
            }

            context.Writer.Write(ArrayTerminator);
            return sizeof(byte) + sizeof(int) + (8 * length) + sizeof(byte);
        }

        public static DateTime[] ReadDateTimeArray(ISerializationContext context)
        {
            ReadAndVerifyElementSize(context, 8);
            int length = context.Reader.ReadInt32();

            DateTime[] values = new DateTime[length];
            for (int i = 0; i < length; ++i)
            {
                values[i] = new DateTime(context.Reader.ReadInt64(), DateTimeKind.Utc);
            }

            ReadAndVerifyTerminator(context);
            return values;
        }

        public static int WriteDateTime(ISerializationContext context, DateTime value)
        {
            context.Writer.Write(value.ToUniversalTime().Ticks);
            return 8;
        }

        public static DateTime ReadDateTime(ISerializationContext context)
        {
            return new DateTime(context.Reader.ReadInt64(), DateTimeKind.Utc);
        }
        #endregion

        #region Guid
        private static int WriteGuidArray(ISerializationContext context, Guid[] array, int index, int length)
        {
            context.Writer.Write((byte)16);
            context.Writer.Write(length);

            int end = index + length;
            for (int i = index; i < end; ++i)
            {
                context.Writer.Write(array[i].ToByteArray());
            }

            context.Writer.Write(ArrayTerminator);
            return sizeof(byte) + sizeof(int) + (16 * length) + sizeof(byte);
        }

        private static Guid[] ReadGuidArray(ISerializationContext context)
        {
            ReadAndVerifyElementSize(context, 16);
            int length = context.Reader.ReadInt32();

            Guid[] values = new Guid[length];
            for (int i = 0; i < length; ++i)
            {
                values[i] = new Guid(context.Reader.ReadBytes(16));
            }

            ReadAndVerifyTerminator(context);
            return values;
        }

        public static int WriteGuid(ISerializationContext context, Guid value)
        {
            context.Writer.Write(value.ToByteArray());
            return 16;
        }

        public static Guid ReadGuid(ISerializationContext context)
        {
            return new Guid(context.Reader.ReadBytes(16));
        }
        #endregion

        #region TimeSpan
        public static int WriteTimeSpanArray(ISerializationContext context, TimeSpan[] array, int index, int length)
        {
            context.Writer.Write((byte)8);
            context.Writer.Write(length);

            int end = index + length;
            for (int i = index; i < end; ++i)
            {
                context.Writer.Write(array[i].Ticks);
            }

            context.Writer.Write(ArrayTerminator);
            return sizeof(byte) + sizeof(int) + (8 * length) + sizeof(byte);
        }

        public static TimeSpan[] ReadTimeSpanArray(ISerializationContext context)
        {
            ReadAndVerifyElementSize(context, 8);
            int length = context.Reader.ReadInt32();

            TimeSpan[] values = new TimeSpan[length];
            for (int i = 0; i < length; ++i)
            {
                values[i] = new TimeSpan(context.Reader.ReadInt64());
            }

            ReadAndVerifyTerminator(context);
            return values;
        }

        public static int WriteTimeSpan(ISerializationContext context, TimeSpan value)
        {
            context.Writer.Write(value.Ticks);
            return 8;
        }

        public static TimeSpan ReadTimeSpan(ISerializationContext context)
        {
            return new TimeSpan(context.Reader.ReadInt64());
        }
        #endregion

        #region string
        public static int WriteStringArray(ISerializationContext context, string[] array, int index, int length)
        {
            context.Writer.Write((byte)255);
            context.Writer.Write(length);

            int end = index + length;
            for (int i = index; i < end; ++i)
            {
                context.Writer.Write(array[i] ?? String.Empty);
            }

            context.Writer.Write(ArrayTerminator);
            return sizeof(byte) + sizeof(int) + (8 * length) + sizeof(byte);
        }

        public static string[] ReadStringArray(ISerializationContext context)
        {
            ReadAndVerifyElementSize(context, 255);
            int length = context.Reader.ReadInt32();

            string[] values = new string[length];
            for (int i = 0; i < length; ++i)
            {
                values[i] = context.Reader.ReadString();
            }

            ReadAndVerifyTerminator(context);
            return values;
        }

        public static int WriteString(ISerializationContext context, string value)
        {
            long before = context.Writer.BaseStream.Position;
            context.Writer.Write(value ?? String.Empty);
            long after = context.Writer.BaseStream.Position;
            return (int)(after - before);
        }

        public static string ReadString(ISerializationContext context)
        {
            return context.Reader.ReadString();
        }
        #endregion

        #region Common Private Methods
        private static void ReadAndVerifyElementSize(ISerializationContext context, byte expectedSize)
        {
            byte elementSize = context.Reader.ReadByte();
            if (elementSize != expectedSize)
            {
                string message = StringExtensions.Format("Expected element size of {0} from binary buffer, instead read element size of {1}", expectedSize, elementSize);
                throw new IOException(message);
            }
        }

        private static void ReadAndVerifyTerminator(ISerializationContext context)
        {
            byte streamTerminator = context.Reader.ReadByte();
            if (streamTerminator != ArrayTerminator)
            {
                string message = StringExtensions.Format("Expected terminator {0:x2}, instead read terminator {1:x2}", ArrayTerminator, streamTerminator);
                throw new IOException(message);
            }
        }
        #endregion
    }
}
