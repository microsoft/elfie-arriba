using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  ITabularValue wraps a single value returned by ITabularReader.
    ///  It's an interface so that sources with different underlying types can
    ///  provide them back without conversion, allocation, or boxing.
    ///  
    ///  NOTE: ITabularReader implementations should cache and reuse the types
    ///  which implement ITabularValue and reuse them for each new row in order
    ///  to properly prevent allocation or boxing.
    /// </summary>
    public interface ITabularValue
    {
        String8 ToString8();
        bool IsNullOrEmpty();

        string ToString();
        
        bool TryToBoolean(out bool result);
        bool TryToInteger(out int result);
        bool TryToDateTime(out DateTime result);
    }

    /// <summary>
    ///  String8TabularValue implements ITabularValue on an underlying String8.
    ///  Reuse instances of String8TabularValue across rows and call SetValue()
    ///  to avoid any per-row allocation.
    /// </summary>
    public class String8TabularValue : ITabularValue
    {
        private String8 _value;

        public String8TabularValue()
        { }

        public void SetValue(String8 value)
        {
            _value = value;
        }

        public String8 ToString8()
        {
            return _value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public bool IsNullOrEmpty()
        {
            return _value.IsEmpty();
        }

        public bool TryToBoolean(out bool result)
        {
            return _value.TryToBoolean(out result);
        }

        public bool TryToDateTime(out DateTime result)
        {
            return _value.TryToDateTime(out result);
        }

        public bool TryToInteger(out int result)
        {
            return _value.TryToInteger(out result);
        }
    }

    /// <summary>
    ///  ObjectTabularValue implements ITabularValue for System.Object.
    /// </summary>
    public class ObjectTabularValue : ITabularValue
    {
        private object _value;
        private String8Block _block;

        public ObjectTabularValue(String8Block convertBuffer)
        {
            _block = convertBuffer;
        }

        public void SetValue(object value)
        {
            _value = value;
        }

        public String8 ToString8()
        {
            return _block.GetCopy(this.ToString());
        }

        public bool IsNullOrEmpty()
        {
            if (_value == null) return true;
            if (_value is string && ((string)_value).Length == 0) return true;
            return false;
        }

        public override string ToString()
        {
            if (_value == null) return null;
            return _value.ToString();
        }

        public bool TryToBoolean(out bool result)
        {
            result = false;
            if (_value == null) return false;

            if(_value is bool)
            {
                result = (bool)_value;
                return true;
            }

            return bool.TryParse(this.ToString(), out result);
        }

        public bool TryToDateTime(out DateTime result)
        {
            result = DateTime.MinValue;
            if (_value == null) return false;

            if(_value is DateTime)
            {
                result = (DateTime)_value;
                return true;
            }

            return DateTime.TryParse(this.ToString(), out result);
        }

        public bool TryToInteger(out int result)
        {
            result = 0;
            if(_value == null) return false;

            if(_value is int)
            {
                result = (int)_value;
                return true;
            }
            else if(_value is uint)
            {
                uint asUint = (uint)_value;
                if(asUint <= int.MaxValue)
                {
                    _value = (int)asUint;
                    return true;
                }

                return false;
            }
            else if (_value is long)
            {
                long asLong = (long)_value;
                if (asLong >= int.MinValue && asLong <= int.MaxValue)
                {
                    _value = (int)asLong;
                    return true;
                }

                return false;
            }
            else if (_value is ulong)
            {
                ulong asULong = (ulong)_value;
                if (asULong <= int.MaxValue)
                {
                    _value = (int)asULong;
                    return true;
                }

                return false;
            }
            else if(_value is short)
            {
                result = (int)_value;
                return true;
            }
            else if (_value is ushort)
            {
                result = (int)_value;
                return true;
            }
            else if (_value is byte)
            {
                result = (int)_value;
                return true;
            }
            else if (_value is sbyte)
            {
                result = (int)_value;
                return true;
            }

            return int.TryParse(this.ToString(), out result);
        }
    }
}
