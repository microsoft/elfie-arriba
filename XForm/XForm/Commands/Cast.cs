// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Commands
{
    internal class CastCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "cast";
        public string Usage => "'cast' [columnName] [targetType] [default?] [strict?]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Cast(source,
                XForm.Functions.Cast.Build(
                    source,
                    context.Parser.NextColumn(source, context),
                    context.Parser.NextType(),
                    (context.Parser.HasAnotherPart ? context.Parser.NextLiteralValue() : null),
                    (context.Parser.HasAnotherPart ? context.Parser.NextBoolean() : false)
                )
            );
        }
    }

    public class Cast : DataBatchEnumeratorWrapper
    {
        private int _sourceColumnIndex;
        private IDataBatchColumn _castedColumn;
        private List<ColumnDetails> _columns;

        public Cast(IDataBatchEnumerator source, IDataBatchColumn castedColumn) : base(source)
        {
            _sourceColumnIndex = source.Columns.IndexOfColumn(castedColumn.ColumnDetails.Name);
            _castedColumn = castedColumn;

            _columns = new List<ColumnDetails>();
            for (int i = 0; i < source.Columns.Count; ++i)
            {
                _columns.Add((i == _sourceColumnIndex ? castedColumn.ColumnDetails : source.Columns[i]));
            }
        }

        public override IReadOnlyList<ColumnDetails> Columns => _columns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _sourceColumnIndex) return _source.ColumnGetter(columnIndex);

            // Pass through the cast for conversions
            return _castedColumn.Getter();
        }
    }
}
