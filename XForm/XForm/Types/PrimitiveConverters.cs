// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Types
{
    internal interface INumericConverter
    {
        bool[] FromSbyte(DataBatch batch, out Array result);
        bool[] FromByte(DataBatch batch, out Array result);
        bool[] FromShort(DataBatch batch, out Array result);
        bool[] FromUshort(DataBatch batch, out Array result);
        bool[] FromInt(DataBatch batch, out Array result);
        bool[] FromUint(DataBatch batch, out Array result);
        bool[] FromLong(DataBatch batch, out Array result);
        bool[] FromUlong(DataBatch batch, out Array result);
        bool[] FromFloat(DataBatch batch, out Array result);
        bool[] FromDouble(DataBatch batch, out Array result);
    }

    internal static class PrimitiveConverterFactory
    {
        public static NegatedTryConvert TryGetNegatedTryConvert(Type fromType, Type toType, object defaultValue)
        {
            if (toType == typeof(sbyte)) return TryGetNegatedTryConvert(fromType, new SbyteConverter(defaultValue));
            if (toType == typeof(byte)) return TryGetNegatedTryConvert(fromType, new ByteConverter(defaultValue));
            if (toType == typeof(short)) return TryGetNegatedTryConvert(fromType, new ShortConverter(defaultValue));
            if (toType == typeof(ushort)) return TryGetNegatedTryConvert(fromType, new UshortConverter(defaultValue));
            if (toType == typeof(int)) return TryGetNegatedTryConvert(fromType, new IntConverter(defaultValue));
            if (toType == typeof(uint)) return TryGetNegatedTryConvert(fromType, new UintConverter(defaultValue));
            if (toType == typeof(long)) return TryGetNegatedTryConvert(fromType, new LongConverter(defaultValue));
            if (toType == typeof(ulong)) return TryGetNegatedTryConvert(fromType, new UlongConverter(defaultValue));
            if (toType == typeof(float)) return TryGetNegatedTryConvert(fromType, new FloatConverter(defaultValue));
            if (toType == typeof(double)) return TryGetNegatedTryConvert(fromType, new DoubleConverter(defaultValue));
            return null;
        }

        public static NegatedTryConvert TryGetNegatedTryConvert(Type fromType, INumericConverter converter)
        {
            if (fromType == typeof(sbyte)) return converter.FromSbyte;
            if (fromType == typeof(byte)) return converter.FromByte;
            if (fromType == typeof(short)) return converter.FromShort;
            if (fromType == typeof(ushort)) return converter.FromUshort;
            if (fromType == typeof(int)) return converter.FromInt;
            if (fromType == typeof(uint)) return converter.FromUint;
            if (fromType == typeof(long)) return converter.FromLong;
            if (fromType == typeof(ulong)) return converter.FromUlong;
            if (fromType == typeof(float)) return converter.FromFloat;
            if (fromType == typeof(double)) return converter.FromDouble;
            return null;
        }
    }

    /// <summary>
    ///  Converter from numeric types to sbyte. [GENERATED]
    /// </summary>
    internal class SbyteConverter : INumericConverter
    {
        private sbyte _defaultValue;
        private sbyte[] _array;
        private bool[] _couldNotConvert;

        public SbyteConverter(object defaultValue)
        {
            _defaultValue = (sbyte)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(sbyte)) ?? default(sbyte));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (sbyte)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                short value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (short)sbyte.MinValue || value > (short)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (int)sbyte.MinValue || value > (int)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)sbyte.MinValue || value > (long)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                byte value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (byte)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ushort value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ushort)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                uint value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (uint)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)sbyte.MinValue || value > (float)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)sbyte.MinValue || value > (double)sbyte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (sbyte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to short. [GENERATED]
    /// </summary>
    internal class ShortConverter : INumericConverter
    {
        private short _defaultValue;
        private short[] _array;
        private bool[] _couldNotConvert;

        public ShortConverter(object defaultValue)
        {
            _defaultValue = (short)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(short)) ?? default(short));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (short)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (short)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (int)short.MinValue || value > (int)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)short.MinValue || value > (long)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (short)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ushort value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ushort)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                uint value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (uint)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)short.MinValue || value > (float)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)short.MinValue || value > (double)short.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (short)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to int. [GENERATED]
    /// </summary>
    internal class IntConverter : INumericConverter
    {
        private int _defaultValue;
        private int[] _array;
        private bool[] _couldNotConvert;

        public IntConverter(object defaultValue)
        {
            _defaultValue = (int)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(int)) ?? default(int));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (int)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (int)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (int)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)int.MinValue || value > (long)int.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (int)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (int)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (int)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                uint value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (uint)int.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (int)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)int.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (int)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)int.MinValue || value > (float)int.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (int)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)int.MinValue || value > (double)int.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (int)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to long. [GENERATED]
    /// </summary>
    internal class LongConverter : INumericConverter
    {
        private long _defaultValue;
        private long[] _array;
        private bool[] _couldNotConvert;

        public LongConverter(object defaultValue)
        {
            _defaultValue = (long)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(long)) ?? default(long));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (long)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)long.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (long)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)long.MinValue || value > (float)long.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (long)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)long.MinValue || value > (double)long.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (long)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to byte. [GENERATED]
    /// </summary>
    internal class ByteConverter : INumericConverter
    {
        private byte _defaultValue;
        private byte[] _array;
        private bool[] _couldNotConvert;

        public ByteConverter(object defaultValue)
        {
            _defaultValue = (byte)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(byte)) ?? default(byte));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                sbyte value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (sbyte)byte.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                short value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (short)byte.MinValue || value > (short)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (int)byte.MinValue || value > (int)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)byte.MinValue || value > (long)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (byte)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ushort value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ushort)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                uint value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (uint)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)byte.MinValue || value > (float)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)byte.MinValue || value > (double)byte.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (byte)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to ushort. [GENERATED]
    /// </summary>
    internal class UshortConverter : INumericConverter
    {
        private ushort _defaultValue;
        private ushort[] _array;
        private bool[] _couldNotConvert;

        public UshortConverter(object defaultValue)
        {
            _defaultValue = (ushort)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(ushort)) ?? default(ushort));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                sbyte value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (sbyte)ushort.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                short value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (short)ushort.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (int)ushort.MinValue || value > (int)ushort.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)ushort.MinValue || value > (long)ushort.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (ushort)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (ushort)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                uint value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (uint)ushort.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)ushort.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)ushort.MinValue || value > (float)ushort.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)ushort.MinValue || value > (double)ushort.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ushort)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to uint. [GENERATED]
    /// </summary>
    internal class UintConverter : INumericConverter
    {
        private uint _defaultValue;
        private uint[] _array;
        private bool[] _couldNotConvert;

        public UintConverter(object defaultValue)
        {
            _defaultValue = (uint)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(uint)) ?? default(uint));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                sbyte value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (sbyte)uint.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                short value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (short)uint.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (int)uint.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)uint.MinValue || value > (long)uint.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (uint)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (uint)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (uint)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                ulong value = sourceArray[batch.Index(i)];
                bool outOfRange = value > (ulong)uint.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)uint.MinValue || value > (float)uint.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)uint.MinValue || value > (double)uint.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (uint)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to ulong. [GENERATED]
    /// </summary>
    internal class UlongConverter : INumericConverter
    {
        private ulong _defaultValue;
        private ulong[] _array;
        private bool[] _couldNotConvert;

        public UlongConverter(object defaultValue)
        {
            _defaultValue = (ulong)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(ulong)) ?? default(ulong));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                sbyte value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (sbyte)ulong.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (ulong)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                short value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (short)ulong.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (ulong)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (int)ulong.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (ulong)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                long value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (long)ulong.MinValue;

                _array[i] = (outOfRange ? _defaultValue : (ulong)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (ulong)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (ulong)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (ulong)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (ulong)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                float value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (float)ulong.MinValue || value > (float)ulong.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ulong)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)ulong.MinValue || value > (double)ulong.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (ulong)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to float. [GENERATED]
    /// </summary>
    internal class FloatConverter : INumericConverter
    {
        private float _defaultValue;
        private float[] _array;
        private bool[] _couldNotConvert;

        public FloatConverter(object defaultValue)
        {
            _defaultValue = (float)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(float)) ?? default(float));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (float)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvert, batch.Count);

            bool couldNotConvertAny = false;
            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                double value = sourceArray[batch.Index(i)];
                bool outOfRange = value < (double)float.MinValue || value > (double)float.MaxValue;

                _array[i] = (outOfRange ? _defaultValue : (float)sourceArray[batch.Index(i)]);
                _couldNotConvert[i] = outOfRange;
                couldNotConvertAny |= outOfRange;
            }

            result = _array;
            return (couldNotConvertAny ? _couldNotConvert : null);
        }
    }

    /// <summary>
    ///  Converter from numeric types to double. [GENERATED]
    /// </summary>
    internal class DoubleConverter : INumericConverter
    {
        private double _defaultValue;
        private double[] _array;


        public DoubleConverter(object defaultValue)
        {
            _defaultValue = (double)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(double)) ?? default(double));
        }

        public bool[] FromSbyte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            sbyte[] sourceArray = (sbyte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromShort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            short[] sourceArray = (short[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromInt(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromLong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            long[] sourceArray = (long[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromByte(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            byte[] sourceArray = (byte[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUshort(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ushort[] sourceArray = (ushort[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUint(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            uint[] sourceArray = (uint[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromUlong(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            ulong[] sourceArray = (ulong[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromFloat(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            float[] sourceArray = (float[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }

        public bool[] FromDouble(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            double[] sourceArray = (double[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _array[i] = (double)sourceArray[batch.Index(i)];
            }

            result = _array;
            return null;
        }
    }
}