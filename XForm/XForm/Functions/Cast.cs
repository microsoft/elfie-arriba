// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;
using XForm.Types;

namespace XForm.Functions
{
    internal class CastBuilder : IFunctionBuilder
    {
        public string Name => "Cast";
        public string Usage => "Cast({Col|Func|Const}, {ToType}, {ErrorOn?}, {DefaultValue?}, {DefaultOn?})";
        public Type ReturnType => null;

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            // Column and ToType are required
            IXColumn column = context.Parser.NextColumn(source, context);
            Type toType = context.Parser.NextType();

            // ErrorOn, DefaultValue, and ChangeToDefaultOn are optional
            ValueKinds errorOn = ValueKindsDefaults.ErrorOn;
            object defaultValue = null;
            ValueKinds changeToDefaultOn = ValueKindsDefaults.ChangeToDefault;

            if (context.Parser.HasAnotherArgument)
            {
                // Parse ErrorOn if there's another argument
                errorOn = context.Parser.NextEnum<ValueKinds>();

                // If there's another argument, both of the last two are required
                if (context.Parser.HasAnotherArgument)
                {
                    defaultValue = context.Parser.NextLiteralValue();
                    changeToDefaultOn = context.Parser.NextEnum<ValueKinds>();
                }
            }

            return CastedColumn.Build(source, column, toType, errorOn, defaultValue, changeToDefaultOn);
        }
    }

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

            // Otherwise, wrap in a CastedColumn [Constants and Enums are automatically converted just once]
            return new CastedColumn(column, targetType, errorOnKinds, defaultValue, changeToDefaultKinds);
        }

        public ArraySelector CurrentSelector => _column.CurrentSelector;

        public Func<XArray> CurrentGetter()
        {
            Func<ArraySelector, XArray> indicesGetter = IndicesGetter();
            if (indicesGetter != null)
            {
                // If the column has fixed values, convert them once and cache them
                ValuesGetter();

                // Get values an indices unmapped and replace the array with the converted array 
                return () =>
                {
                    XArray unmapped = indicesGetter(CurrentSelector);
                    return XArray.All(_convertedValues.Array, _convertedValues.Count).Reselect(unmapped.Selector);
                };
            }
            else
            {
                // Otherwise, convert from the underlying current getter
                Func<XArray> sourceGetter = _column.CurrentGetter();
                return () => _converter(sourceGetter());
            }
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> indicesGetter = IndicesGetter();
            if(indicesGetter != null)
            {
                // If the column has fixed values, convert them once and cache them
                ValuesGetter();

                // Get values an indices unmapped and replace the array with the converted array 
                return (selector) =>
                {
                    XArray unmapped = indicesGetter(selector);
                    return XArray.All(_convertedValues.Array, _convertedValues.Count).Reselect(unmapped.Selector);
                };
            }
            else
            {
                // Otherwise, convert from the underlying seek getter
                Func<ArraySelector, XArray> sourceGetter = _column.SeekGetter();
                if (sourceGetter == null) return null;

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

        public Func<ArraySelector, XArray> IndicesGetter()
        {
            return _column.IndicesGetter();
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
