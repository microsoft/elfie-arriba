using System;
using XForm.Data;
using XForm.Types;

namespace XForm.Functions
{
    internal class CastBuilder : IFunctionBuilder
    {
        public string Name => "Cast";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Cast(
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

        public Cast(IDataBatchColumn column, Type targetType, object defaultValue, bool strict)
        {
            Column = column;
            ColumnDetails = column.ColumnDetails.ChangeType(targetType);
            Converter = TypeConverterFactory.GetConverter(column.ColumnDetails.Type, targetType, defaultValue, strict);
        }

        public Func<DataBatch> Getter()
        {
            Func<DataBatch> sourceGetter = Column.Getter();
            return () => Converter(sourceGetter());
        }
    }
}
