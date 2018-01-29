// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Types;

namespace XForm.Columns
{
    /// <summary>
    ///  Cast converts values from one type to another.
    ///  If passed only a column and target type, it will convert valid values and turn invalid values, empty strings, and nulls into Null.
    ///  
    ///  Likely Desired Forms:
    ///    Cast([Column], [Type])                                   -> 'Safe'. Invalid/Null/Empty turn into Null.
    ///    Cast([Column], [Type], NullOrInvalid)                    -> 'Strict'. Error on Invalid, Null, or Empty values.
    ///    Cast([Column], [Type], Invalid)                          -> Throw on invalid values, but all empty/null to pass through.
    ///    Cast([Column], [Type], None, [Default], Invalid)         -> Leave nulls alone but replaced invalid values with the default.
    ///    Cast([Column], [Type], None, [Default], NullOrInvalid)   -> 'Loose'. Convert Invalid, Null, and Empty to the default.
    /// </summary>
    public class CastedColumn : IXColumn
    {
        private IXColumn _column;
        public ColumnDetails ColumnDetails { get; private set; }

        private Func<XArray, XArray> _converter;
        private XArray _convertedValues;

        private Type _targetType;
        private ValueKinds _errorOnKinds;
        private object _defaultValue;
        private ValueKinds _changeToDefaultKinds;

        private CastedColumn(IXColumn column, Type targetType, ValueKinds errorOnKinds = ValueKindsDefaults.ErrorOn, object defaultValue = null, ValueKinds changeToDefaultKinds = ValueKindsDefaults.ChangeToDefault)
        {
            _column = column;
            ColumnDetails = column.ColumnDetails.ChangeType(targetType);
            _converter = TypeConverterFactory.GetConverter(column.ColumnDetails.Type, targetType, errorOnKinds, defaultValue, changeToDefaultKinds);

            _targetType = targetType;
            _errorOnKinds = errorOnKinds;
            _defaultValue = defaultValue;
            _changeToDefaultKinds = changeToDefaultKinds;
        }

        public static IXColumn Build(IXTable source, IXColumn column, Type targetType, ValueKinds errorOnKinds = ValueKindsDefaults.ErrorOn, object defaultValue = null, ValueKinds changeToDefaultKinds = ValueKindsDefaults.ChangeToDefault)
        {
            // If the column is already the right type, just return it
            if (column.ColumnDetails.Type == targetType) return column;

            // Convert constants individually
            if (column.IsConstantColumn())
            {
                XArray array = column.ValuesGetter()();
                return new ConstantColumn(source, TypeConverterFactory.ConvertSingle(array.Array.GetValue(0), targetType), targetType);
            }

            // Otherwise, wrap in a CastedColumn [Enums are automatically converted just once]
            return new CastedColumn(column, targetType, errorOnKinds, defaultValue, changeToDefaultKinds);
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            if (ValuesGetter() != null)
            {
                // Get values mapped and replace values array with the converted array 
                return () => sourceGetter().ReplaceValues(_convertedValues);
            }
            else
            {
                // Otherwise, convert from the underlying current getter
                return () => _converter(sourceGetter());
            }
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> sourceGetter = _column.SeekGetter();
            if (sourceGetter == null) return null;

            if (ValuesGetter() != null)
            {
                // Get values mapped and replace values array with the converted array 
                return (selector) => sourceGetter(selector).ReplaceValues(_convertedValues);
            }
            else
            {
                // Otherwise, convert from the underlying seek getter
                return (selector) => _converter(sourceGetter(selector));
            }
        }

        public Func<XArray> ValuesGetter()
        {
            if (_convertedValues.Array == null)
            {
                Func<XArray> innerGetter = _column.ValuesGetter();
                if (innerGetter == null) return null;

                _convertedValues = _converter(innerGetter());
            }

            return () => _convertedValues;
        }

        public Type IndicesType => _column.IndicesType;

        public Func<XArray> IndicesCurrentGetter()
        {
            return _column.IndicesCurrentGetter();
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return _column.IndicesSeekGetter();
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return _column.ComponentGetter(componentName);
        }

        public override string ToString()
        {
            if (_defaultValue != null && _changeToDefaultKinds != ValueKinds.InvalidOrNull)
            {
                return $"Cast({_column}, {_targetType.Name}, {_errorOnKinds}, {XqlScanner.Escape(_defaultValue, TokenType.Value)}, {_changeToDefaultKinds})";
            }
            else if (_errorOnKinds != ValueKinds.None)
            {
                return $"Cast({_column}, {_targetType.Name}, {_errorOnKinds})";
            }
            else
            {
                return $"Cast({_column}, {_targetType.Name})";
            }
        }
    }
}
