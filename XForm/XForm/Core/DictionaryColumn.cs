// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Types;

namespace XForm
{
    public interface IDictionaryColumn
    {
        int Length { get; }
        Array Values { get; }
        void Reset(int size);
        int HashCurrent(int hash);
        void SetArray(XArray xarray);
        void SetCurrent(uint index);
        void SwapCurrent(uint index);
        bool EqualsCurrent(uint index);
        bool BetterThanCurrent(uint index, ChooseDirection direction);
    }

    internal class DictionaryColumn<TColumnType> : IDictionaryColumn
    {
        private IXArrayComparer<TColumnType> _comparer;
        private IValueCopier<TColumnType> _copier;

        private TColumnType[] _values;
        private TColumnType _current;
        private bool _currentIsNewValue;

        private XArray _currentArray;
        private TColumnType[] _currentTypedArray;

        public DictionaryColumn()
        {
            ITypeProvider typeProvider = TypeProviderFactory.Get(typeof(TColumnType));
            _comparer = (IXArrayComparer<TColumnType>)typeProvider.TryGetComparer();
            _copier = (IValueCopier<TColumnType>)typeProvider.TryGetCopier();
        }

        public int Length => _values.Length;
        public Array Values => _values;

        public void Reset(int size)
        {
            _values = new TColumnType[size];
        }

        public int HashCurrent(int hash)
        {
            return (hash << 5) - hash + _comparer.GetHashCode(_current);
        }

        public void SetArray(XArray xarray)
        {
            _currentArray = xarray;
            _currentTypedArray = (TColumnType[])xarray.Array;
        }

        public void SetCurrent(uint index)
        {
            _currentIsNewValue = true;

            int realIndex = _currentArray.Index((int)index);
            if (_currentArray.HasNulls && _currentArray.NullRows[realIndex])
            {
                _current = default(TColumnType);
            }
            else
            {
                _current = _currentTypedArray[realIndex];
            }
        }

        public bool EqualsCurrent(uint index)
        {
            return _comparer.WhereEqual(_current, _values[index]);
        }

        public bool BetterThanCurrent(uint index, ChooseDirection direction)
        {
            if (direction == ChooseDirection.Max)
            {
                return _comparer.WhereGreaterThan(_current, _values[index]);
            }
            else
            {
                return _comparer.WhereLessThan(_current, _values[index]);
            }
        }

        public void SwapCurrent(uint index)
        {
            // Copy only new values and only when they become used
            if (_currentIsNewValue && _copier != null)
            {
                _current = _copier.Copy(_current);
                _currentIsNewValue = false;
            }

            TColumnType temp = _values[index];
            _values[index] = _current;
            _current = temp;
        }
    }
}
