// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;

namespace Arriba.Structures
{
    /// <summary>
    ///  Value handles values of any supported Arriba column type and converts
    ///  values passed to any Arriba type to which it can be converted. Value
    ///  handles parsing strings into DateTimes, Guids, and numbers, for example.
    ///  
    ///  ISSUES:
    ///    - Value causes boxing of value types [ValueTypeReference can be used to workaround]
    ///    - Value tries many conversions even if you only want one
    /// </summary>
    public class Value
    {
        private object _value;
        private object _cachedIdealTypeValue;
        private ByteBlock _cachedByteBlock;
        private int _cachedHashCode;

        private Value(object o)
        {
            Assign(o);
        }

        public static Value Create(object o)
        {
            if (o is Value) return (Value)o;
            return new Value(o);
        }

        public void Assign(object o)
        {
            // Unwrap ValueTypeReference<object> *first*
            if (o is ValueTypeReference<object>)
            {
                o = (o as ValueTypeReference<object>).Value;
            }

            // Copy properties if o is already a Value
            if (o != null && o is Value)
            {
                Value v = (Value)o;
                _value = v._value;
                _cachedByteBlock = v._cachedByteBlock;
                _cachedIdealTypeValue = v._cachedIdealTypeValue;
                _cachedHashCode = v._cachedHashCode;
            }
            else
            {
                _value = o;
                _cachedByteBlock = ByteBlock.Zero;
                _cachedIdealTypeValue = null;
                _cachedHashCode = 0;
            }
        }

        #region Type Determination
        /// <summary>
        ///  Convert the value to the ideal type supported by Arriba.
        ///  
        ///  Types directly supported are kept as-is.
        ///  Numbers are canonicalized to double or long.
        ///  ULongs not fitting in long as ulong.
        ///  Strings parsable as other supported types are converted.
        ///  ByteBlocks and byte[] are ByteBlocks.
        ///  All other types are stored as string.
        /// </summary>
        /// <returns>this.value converted to best supported type</returns>
        private object DetermineIdealTypeValue()
        {
            // If null, ideal is null
            if (_value == null) return null;

            // NOTE: This function can and will be called in parallel on multiple threads
            // so state tracking needs to be done with local variables.
            object idealValueType = _cachedIdealTypeValue;

            // If already determined, return
            if (idealValueType != null) return idealValueType;

            // Consider types already ideal
            if (idealValueType == null) idealValueType = TryAsIs();

            // Convert number types to double/long/ulong
            if (idealValueType == null) idealValueType = TryAsNumber();

            // Consider parsing string to other types
            if (idealValueType == null) idealValueType = TryAsString();

            // Store as ToString of value
            if (idealValueType == null) idealValueType = _value.ToString();

            // Save the computed value into the destination, for integral types Interlocked.Exchange is not strictly needed 
            // but for some types it might be so using it just to be safe
            Interlocked.Exchange(ref _cachedIdealTypeValue, idealValueType);

            return idealValueType;
        }

        private object TryAsIs()
        {
            if (_value is DateTime)
            {
                return _value;
            }
            else if (_value is ValueTypeReference<DateTime>)
            {
                return ((ValueTypeReference<DateTime>)_value).Value;
            }
            else if (_value is bool)
            {
                return _value;
            }
            else if (_value is ValueTypeReference<bool>)
            {
                return ((ValueTypeReference<bool>)_value).Value;
            }
            else if (_value is Guid)
            {
                return _value;
            }
            else if (_value is ValueTypeReference<Guid>)
            {
                return ((ValueTypeReference<Guid>)_value).Value;
            }
            else if (_value is TimeSpan)
            {
                return _value;
            }
            else if (_value is ValueTypeReference<TimeSpan>)
            {
                return ((ValueTypeReference<TimeSpan>)_value).Value;
            }

            return null;
        }

        /// <summary>
        /// Convert the number to the ideal value type stored by Arriba.
        /// For reals, this is double
        /// For integers, this is long
        /// </summary>
        /// <remarks>
        /// Unfortunately, the casting necessarily unboxes/reboxes values causing some inefficiency at runtime.
        /// We can avoid that when the types are already ideal (long and double) which makes those values slightly more 
        /// performant than other types.
        /// </remarks>
        /// <returns></returns>
        private object TryAsNumber()
        {
            if (_value is double)
            {
                return _value;
            }
            else if (_value is ValueTypeReference<double>)
            {
                return ((ValueTypeReference<double>)_value).Value;
            }
            else if (_value is float)
            {
                return (double)(float)_value;
            }
            else if (_value is ValueTypeReference<float>)
            {
                return (double)((ValueTypeReference<float>)_value).Value;
            }
            else if (_value is long)
            {
                return _value;
            }
            else if (_value is ValueTypeReference<long>)
            {
                return ((ValueTypeReference<long>)_value).Value;
            }
            else if (_value is uint)
            {
                return (long)(uint)_value;
            }
            else if (_value is ValueTypeReference<uint>)
            {
                return (long)((ValueTypeReference<uint>)_value).Value;
            }
            else if (_value is int)
            {
                return (long)(int)_value;
            }
            else if (_value is ValueTypeReference<int>)
            {
                return (long)((ValueTypeReference<int>)_value).Value;
            }
            else if (_value is ushort)
            {
                return (long)(ushort)_value;
            }
            else if (_value is ValueTypeReference<ushort>)
            {
                return (long)((ValueTypeReference<ushort>)_value).Value;
            }
            else if (_value is short)
            {
                return (long)(short)_value;
            }
            else if (_value is ValueTypeReference<short>)
            {
                return (long)((ValueTypeReference<short>)_value).Value;
            }
            else if (_value is sbyte)
            {
                return (long)(sbyte)_value;
            }
            else if (_value is ValueTypeReference<sbyte>)
            {
                return (long)((ValueTypeReference<sbyte>)_value).Value;
            }
            else if (_value is byte)
            {
                return (long)(byte)_value;
            }
            else if (_value is ValueTypeReference<byte>)
            {
                return (long)((ValueTypeReference<byte>)_value).Value;
            }
            else if (_value is ulong)
            {
                ulong asUlong = (ulong)_value;
                if (asUlong > long.MaxValue)
                {
                    return asUlong;
                }
                else
                {
                    return (long)asUlong;
                }
            }
            else if (_value is ValueTypeReference<ulong>)
            {
                ulong asUlong = ((ValueTypeReference<ulong>)_value).Value;
                if (asUlong > long.MaxValue)
                {
                    return asUlong;
                }
                else
                {
                    return (long)asUlong;
                }
            }

            return null;
        }

        private object TryAsString()
        {
            string asString;

            // Try to get string representation of value
            if (_value is string)
            {
                asString = (string)_value;
            }
            else if (_value is ValueTypeReference<string>)
            {
                asString = (_value as ValueTypeReference<string>).Value;
            }
            else if (_value is ByteBlock)
            {
                asString = ((ByteBlock)_value).ToString();
            }
            else if (_value is ValueTypeReference<ByteBlock>)
            {
                asString = (_value as ValueTypeReference<ByteBlock>).Value.ToString();
            }
            else
            {
                return null;
            }

            if (String.IsNullOrEmpty(asString)) return asString;

            // If gotten, try parsing conversions to other types
            DateTime asDateTime = default(DateTime);
            Guid asGuid = default(Guid);
            bool asBool = default(bool);
            double asDouble = default(double);
            long asLong = default(long);
            ulong asULong = default(ulong);
            TimeSpan asTimeSpan = default(TimeSpan);

            if (DateTime.TryParse(asString, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out asDateTime))
            {
                return asDateTime;
            }
            else if (Guid.TryParse(asString, out asGuid))
            {
                return asGuid;
            }
            else if (bool.TryParse(asString, out asBool))
            {
                return asBool;
            }
            else if (long.TryParse(asString, out asLong))
            {
                return asLong;
            }
            else if (ulong.TryParse(asString, out asULong))
            {
                return asULong;
            }
            else if ((asString.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) && ulong.TryParse(asString.TrimStart('0', 'x', 'X'), NumberStyles.HexNumber, null, out asULong))
            {
                return asULong;
            }
            else if (double.TryParse(asString, out asDouble))
            {
                return asDouble;
            }
            else if (TimeSpan.TryParse(asString, out asTimeSpan))
            {
                // NOTE: TimeSpan must be after numeric types so that plain numbers are preferred as numeric types.
                // If some values to bestType can only be TimeSpans, that type will be picked.
                return asTimeSpan;
            }
            else
            {
                return asString;
            }
        }
        #endregion

        #region BestType
        /// <summary>
        ///  Return the best type for this Value. This is the recommended
        ///  Arriba column type for values like this. Values which happen
        ///  to be specific types like DateTime or Guid are recommended as
        ///  that. Numeric values are recommended as the smallest floating or
        ///  integer column type which will store them down to 32 bits.
        /// </summary>
        /// <returns>Recommended Type to store values like this</returns>
        public Type BestType()
        {
            // If null, return object to hint anything may be valid
            if (_value == null) return typeof(object);

            // Consider conversions to determine the ideal type
            object idealTypeValue = this.DetermineIdealTypeValue();

            if (idealTypeValue is long)
            {
                // For integer types, suggest int if value in range, otherwise long
                long asLong = (long)idealTypeValue;
                if (asLong >= int.MinValue && asLong <= int.MaxValue)
                {
                    return typeof(int);
                }

                return typeof(long);
            }
            else if (idealTypeValue is double)
            {
                // For floating point types, suggest float if value in range, otherwise double
                double asDouble = (double)idealTypeValue;
                if (asDouble >= float.MinValue && asDouble <= float.MaxValue)
                {
                    return typeof(float);
                }

                return typeof(double);
            }
            else if (idealTypeValue is string)
            {
                // For strings, return object for null/empty string to indicate type still open-ended
                string asString = (string)idealTypeValue;
                if (String.IsNullOrEmpty(asString))
                {
                    return typeof(object);
                }
            }

            // Otherwise, return type exactly as determined
            return idealTypeValue.GetType();
        }

        /// <summary>
        ///  Return the recommended column type given the recommended type so
        ///  far and this additional value.
        /// </summary>
        /// <param name="bestSoFar">Type recommended so far</param>
        /// <returns>Best type fitting both bestSoFar and this value</returns>
        public Type BestType(Type bestSoFar)
        {
            // If there's an existing best and this value converts to it, keep it
            object unused;
            if (bestSoFar != null && TryConvert(bestSoFar, out unused)) return bestSoFar;

            Type thisBest = this.BestType();

            // If this is object, stick with bestSoFar
            if (thisBest.Equals(typeof(object))) return bestSoFar;

            // If bestSoFar is null, use this type
            if (bestSoFar == null) return thisBest;

            // If the same, return the same
            if (thisBest.Equals(bestSoFar)) return bestSoFar;

            // If either is string, it must be string
            if (thisBest.Equals(typeof(string)) || bestSoFar.Equals(typeof(string))) return typeof(string);

            // Allow converting to 'broader' types (int -> long, float -> double, int -> float)
            if(bestSoFar.Equals(typeof(long)))
            {
                if (thisBest.Equals(typeof(int))) return typeof(long);
                if (thisBest.Equals(typeof(float))) return typeof(float);
                if (thisBest.Equals(typeof(double))) return typeof(double);
            }
            else if(bestSoFar.Equals(typeof(int)))
            {
                if (thisBest.Equals(typeof(long))) return typeof(long);
                if (thisBest.Equals(typeof(float))) return typeof(float);
                if (thisBest.Equals(typeof(double))) return typeof(double);

                if (thisBest.Equals(typeof(TimeSpan))) return typeof(TimeSpan);
            }
            else if (bestSoFar.Equals(typeof(float)))
            {
                if (thisBest.Equals(typeof(int))) return typeof(float);
                if (thisBest.Equals(typeof(long))) return typeof(float);
                if (thisBest.Equals(typeof(double))) return typeof(double);
            }
            else if(bestSoFar.Equals(typeof(double)))
            {
                if (thisBest.Equals(typeof(int))) return typeof(double);
                if (thisBest.Equals(typeof(long))) return typeof(double);
                if (thisBest.Equals(typeof(float))) return typeof(double);
            }
            else if(bestSoFar.Equals(typeof(TimeSpan)))
            {
                if (thisBest.Equals(typeof(int))) return typeof(TimeSpan);
            }

            // Otherwise, must fall back to string
            return typeof(string);
        }
        #endregion

        #region TryConvert
        /// <summary>
        ///  Convert this Value to the requested type, if possible, and return
        ///  whether successful.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="result">Converted Value or default(T) if not convertable.</param>
        /// <returns>True if conversion possible, False if not</returns>
        public bool TryConvert<T>(out T result)
        {
            object resultObject;
            if (TryConvert(typeof(T), out resultObject))
            {
                result = (T)resultObject;
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        ///  Convert this Value to the requested type, if possible, and return
        ///  whether successful.
        /// </summary>
        /// <param name="t">Type to convert to</typeparam>
        /// <param name="result">Converted Value or default(T) if not convertable.</param>
        /// <returns>True if conversion possible, False if not</returns>
        public bool TryConvert(Type t, out object result)
        {
            // Null never converts
            if (_value == null)
            {
                result = null;
                return false;
            }

            // If original value already right, return as-is
            if (_value.GetType().Equals(t))
            {
                result = _value;
                return true;
            }

            // Directly convert to string if requested without determination
            if (t == typeof(string))
            {
                if (_value is DateTime)
                {
                    result = ((DateTime)_value).ToString("u", CultureInfo.InvariantCulture);
                    return true;
                }
                else if (_value is ValueTypeReference<DateTime>)
                {
                    result = (_value as ValueTypeReference<DateTime>).Value.ToString("u", CultureInfo.InvariantCulture);
                    return true;
                }
                else
                {
                    result = _value.ToString();
                    return true;
                }
            }

            // Directly convert to ByteBlock if requested without determination
            if (t == typeof(ByteBlock))
            {
                result = ToByteBlock();
                return true;
            }

            // If this is currently a ByteBlock and we didn't want that, retry conversion as a string
            if (_value is ByteBlock)
            {
                _value = _value.ToString();
            }

            // Otherwise, use type determination to detect
            object idealTypeValue = DetermineIdealTypeValue();

            // If the ideal type was requested, return as determined
            if (idealTypeValue.GetType().Equals(t))
            {
                result = idealTypeValue;
                return true;
            }

            // If boolean and another type requested, offer as a number
            if (idealTypeValue is bool)
            {
                idealTypeValue = ((bool)idealTypeValue) ? 1 : 0;
            }

            // Upconvert numbers and downconvert if within range
            if (idealTypeValue is double)
            {
                double asDouble = (double)idealTypeValue;

                if (t == typeof(float))
                {
                    if (asDouble >= float.MinValue && asDouble <= float.MaxValue)
                    {
                        result = (float)asDouble;
                        return true;
                    }
                }
            }
            else if (idealTypeValue is ulong)
            {
                ulong asULong = (ulong)idealTypeValue;
                if (t == typeof(float))
                {
                    result = (float)asULong;
                    return true;
                }
                else if (t == typeof(double))
                {
                    result = (double)asULong;
                    return true;
                }
            }
            else if (idealTypeValue is long)
            {
                long asLong = (long)idealTypeValue;

                if (t == typeof(ulong))
                {
                    if (asLong >= 0)
                    {
                        result = (ulong)asLong;
                        return true;
                    }
                }
                else if (t == typeof(uint))
                {
                    if (asLong >= uint.MinValue && asLong <= uint.MaxValue)
                    {
                        result = (uint)asLong;
                        return true;
                    }
                }
                else if (t == typeof(int))
                {
                    if (asLong >= int.MinValue && asLong <= int.MaxValue)
                    {
                        result = (int)asLong;
                        return true;
                    }
                }
                else if (t == typeof(ushort))
                {
                    if (asLong >= ushort.MinValue && asLong <= ushort.MaxValue)
                    {
                        result = (ushort)asLong;
                        return true;
                    }
                }
                else if (t == typeof(short))
                {
                    if (asLong >= short.MinValue && asLong <= short.MaxValue)
                    {
                        result = (short)asLong;
                        return true;
                    }
                }
                else if (t == typeof(sbyte))
                {
                    if (asLong >= sbyte.MinValue && asLong <= sbyte.MaxValue)
                    {
                        result = (sbyte)asLong;
                        return true;
                    }
                }
                else if (t == typeof(byte))
                {
                    if (asLong >= byte.MinValue && asLong <= byte.MaxValue)
                    {
                        result = (byte)asLong;
                        return true;
                    }
                }
                else if (t == typeof(float))
                {
                    result = (float)asLong;
                    return true;
                }
                else if (t == typeof(double))
                {
                    result = (double)asLong;
                    return true;
                }
                else if (t == typeof(bool))
                {
                    if (asLong == 0)
                    {
                        result = false;
                        return true;
                    }
                    else if (asLong == 1)
                    {
                        result = true;
                        return true;
                    }
                }
            }

            // Consider non-preferred string parse options
            if (_value is string)
            {
                if (t == typeof(TimeSpan))
                {
                    TimeSpan asTimeSpan;
                    bool isTimeSpan = TimeSpan.TryParse((string)_value, out asTimeSpan);
                    result = asTimeSpan;
                    return isTimeSpan;
                }
            }

            // Otherwise, report not convertible
            result = null;
            return false;
        }

        /// <summary>
        ///  Return the ByteBlock representation of the current value (cached).
        /// </summary>
        /// <returns>ByteBlock equivalent of value</returns>
        private ByteBlock ToByteBlock()
        {
            if (_cachedByteBlock.IsZero())
            {
                if (_value is ByteBlock)
                {
                    _cachedByteBlock = (ByteBlock)_value;
                }
                if (_value is ValueTypeReference<ByteBlock>)
                {
                    _cachedByteBlock = (_value as ValueTypeReference<ByteBlock>).Value;
                }
                else if (_value is string)
                {
                    _cachedByteBlock = (ByteBlock)(string)_value;
                }
                else if (_value is byte[])
                {
                    _cachedByteBlock = (ByteBlock)(byte[])_value;
                }
                else if (_value is DateTime)
                {
                    _cachedByteBlock = (ByteBlock)(((DateTime)_value).ToString("u", CultureInfo.InvariantCulture));
                }
                else if (_value is ValueTypeReference<DateTime>)
                {
                    _cachedByteBlock = (ByteBlock)((_value as ValueTypeReference<DateTime>).Value.ToString("u", CultureInfo.InvariantCulture));
                }
                else
                {
                    _cachedByteBlock = (ByteBlock)_value.ToString();
                }
            }

            return _cachedByteBlock;
        }
        #endregion

        #region Object Overrides
        public override bool Equals(object obj)
        {
            Value other = Value.Create(obj);
            if (this.GetHashCode().Equals(other.GetHashCode()))
            {
                return object.Equals(this.DetermineIdealTypeValue(), other.DetermineIdealTypeValue());
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            // Return zero for null
            if (_value == null) return 0;

            // Return previous value if determined
            if (_cachedHashCode != 0) return _cachedHashCode;

            // If ByteBlock, hash directly
            if (_value is ByteBlock)
            {
                _cachedHashCode = ((ByteBlock)_value).GetHashCode();
                return _cachedHashCode;
            }

            if (_value is ValueTypeReference<int>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((long)(_value as ValueTypeReference<int>).Value, 0));
            }
            else if (_value is ValueTypeReference<uint>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((ulong)(_value as ValueTypeReference<uint>).Value, 0));
            }
            else if (_value is ValueTypeReference<long>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((_value as ValueTypeReference<long>).Value, 0));
            }
            else if (_value is ValueTypeReference<double>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((_value as ValueTypeReference<double>).Value, 0));
            }
            else if (_value is ValueTypeReference<ulong>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((_value as ValueTypeReference<ulong>).Value, 0));
            }
            else if (_value is ValueTypeReference<DateTime>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((_value as ValueTypeReference<DateTime>).Value, 0));
            }
            else if (_value is ValueTypeReference<TimeSpan>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((_value as ValueTypeReference<TimeSpan>).Value, 0));
            }
            else if (_value is ValueTypeReference<Guid>)
            {
                _cachedHashCode = unchecked((int)Hashing.MurmurHash3((_value as ValueTypeReference<Guid>).Value, 0));
            }
            else
            {
                // Determine ideal value to hash
                object idealTypeValue = DetermineIdealTypeValue();

                if (idealTypeValue is long)
                {
                    _cachedHashCode = unchecked((int)Hashing.MurmurHash3((long)idealTypeValue, 0));
                }
                else if (idealTypeValue is double)
                {
                    _cachedHashCode = unchecked((int)Hashing.MurmurHash3((double)idealTypeValue, 0));
                }
                else if (idealTypeValue is ulong)
                {
                    _cachedHashCode = unchecked((int)Hashing.MurmurHash3((ulong)idealTypeValue, 0));
                }
                else if (idealTypeValue is DateTime)
                {
                    _cachedHashCode = unchecked((int)Hashing.MurmurHash3((DateTime)idealTypeValue, 0));
                }
                else if (idealTypeValue is TimeSpan)
                {
                    _cachedHashCode = unchecked((int)Hashing.MurmurHash3((TimeSpan)idealTypeValue, 0));
                }
                else if (idealTypeValue is Guid)
                {
                    _cachedHashCode = unchecked((int)Hashing.MurmurHash3((Guid)idealTypeValue, 0));
                }
                else
                {
                    _cachedHashCode = ToByteBlock().GetHashCode();
                }
            }
            return _cachedHashCode;
        }

        public override string ToString()
        {
            return (_value ?? "null").ToString();
        }
        #endregion
    }

    /// <summary>
    /// ValueTypeReference is a reference type that holds another object (primarily a ValueType/struct) similar to a boxed reference.
    /// </summary>
    /// <remarks>
    /// This class is primarily intended for use when populating datablocks for table.AddOrUpdate of value types (non-strings) to eliminate boxing.  
    /// By using this object insertion code can control how often and at what timing allocations/frees occur instead of leaving it up to boxing/unboxing
    /// and the associated GCs.  This object makes little sense for reference types but cannot be constrained further due to usage within Arriba.
    /// </remarks>
    /// <typeparam name="T">type of object to wrap, normally a value type</typeparam>
    public class ValueTypeReference<T>
    {
        public ValueTypeReference()
        {
        }

        public ValueTypeReference(T initialValue)
        {
            this.Value = initialValue;
        }

        public T Value;

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
