// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Types;

namespace XForm.Functions
{
    internal class CastBuilder : IFunctionBuilder
    {
        public string Name => "Cast";
        public string Usage => "Cast([ColumnFunctionOrLiteral], [TargetType], [DefaultValue?], [Strict?])";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return Cast.Build(
                source,
                context.Parser.NextColumn(source, context),
                context.Parser.NextType(),
                (context.Parser.HasAnotherArgument ? context.Parser.NextLiteralValue() : null),
                (context.Parser.HasAnotherArgument ? context.Parser.NextBoolean() : false));
        }
    }

    public class Cast : IDataBatchColumn
    {
        private IDataBatchColumn Column { get; set; }
        private Func<DataBatch, DataBatch> Converter { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private Cast(IDataBatchColumn column, Type targetType, object defaultValue, bool strict)
        {
            Column = column;
            ColumnDetails = column.ColumnDetails.ChangeType(targetType);
            Converter = TypeConverterFactory.GetConverter(column.ColumnDetails.Type, targetType, defaultValue, strict);
        }

        public static IDataBatchColumn Build(IDataBatchEnumerator source, IDataBatchColumn column, Type targetType, object defaultValue, bool strict)
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
                return new Cast(column, targetType, defaultValue, strict);
            }
        }

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = Column.Getter();
            return () => Converter(sourceGetter());
        }
    }
}
