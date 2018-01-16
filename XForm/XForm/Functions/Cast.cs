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
        public string Usage => "Cast([Column|Function|Literal], [ToType], [ErrorOn?], [DefaultValue?], [ChangeToDefaultOn?])";
        public Type ReturnType => null;

        public IDataBatchColumn Build(IDataBatchEnumerator source, XDatabaseContext context)
        {
            // Column and ToType are required
            IDataBatchColumn column = context.Parser.NextColumn(source, context);
            Type toType = context.Parser.NextType();

            // ErrorOn, DefaultValue, and ChangeToDefaultOn are optional
            ValueKinds errorOn = ValueKinds.ErrorOnDefault;
            object defaultValue = null;
            ValueKinds changeToDefaultOn = ValueKinds.ChangeToDefaultOnDefault;

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

            return Cast.Build(source, column, toType, errorOn, defaultValue, changeToDefaultOn);
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
    public class Cast : IDataBatchColumn
    {
        public ColumnDetails ColumnDetails { get; private set; }
        private IDataBatchColumn Column { get; set; }
        private Func<DataBatch, DataBatch> Converter { get; set; }

        private Type TargetType { get; set; }
        private ValueKinds ErrorOnKinds { get; set; }
        private object DefaultValue { get; set; }
        private ValueKinds ChangeToDefaultKinds { get; set; }

        private Cast(IDataBatchColumn column, Type targetType, ValueKinds errorOnKinds = ValueKinds.ErrorOnDefault, object defaultValue = null, ValueKinds changeToDefaultKinds = ValueKinds.ChangeToDefaultOnDefault)
        {
            Column = column;
            ColumnDetails = column.ColumnDetails.ChangeType(targetType);
            Converter = TypeConverterFactory.GetConverter(column.ColumnDetails.Type, targetType, errorOnKinds, defaultValue, changeToDefaultKinds);

            TargetType = targetType;
            ErrorOnKinds = errorOnKinds;
            DefaultValue = defaultValue;
            ChangeToDefaultKinds = changeToDefaultKinds;
        }

        public static IDataBatchColumn Build(IDataBatchEnumerator source, IDataBatchColumn column, Type targetType, ValueKinds errorOnKinds = ValueKinds.ErrorOnDefault, object defaultValue = null, ValueKinds changeToDefaultKinds = ValueKinds.ChangeToDefaultOnDefault)
        {
            // If the column is already the right type, just return it
            if (column.ColumnDetails.Type == targetType) return column;

            if (column is Constant)
            {
                // If the inner value is a constant, convert once and store the new constant
                return new Constant(source, TypeConverterFactory.ConvertSingle(((Constant)column).Value, targetType), targetType);
            }
            else
            {
                return new Cast(column, targetType, errorOnKinds, defaultValue, changeToDefaultKinds);
            }
        }

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = Column.Getter();
            return () => Converter(sourceGetter());
        }

        public override string ToString()
        {
            if (DefaultValue != null && ChangeToDefaultKinds != ValueKinds.InvalidOrNull)
            {
                return $"Cast({Column}, {TargetType.Name}, {ErrorOnKinds}, {XqlScanner.Escape(DefaultValue, TokenType.Value)}, {ChangeToDefaultKinds})";
            }
            else if (ErrorOnKinds != ValueKinds.None)
            {
                return $"Cast({Column}, {TargetType.Name}, {ErrorOnKinds})";
            }
            else
            {
                return $"Cast({Column}, {TargetType.Name})";
            }
        }
    }
}
