// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.Functions;
using XForm.Query;

namespace XForm.Verbs
{
    internal class CastCommandBuilder : IVerbBuilder
    {
        public string Verb => "cast";
        public string Usage => "cast {Column} {ToType} {ErrorOn?} {DefaultValue?} {DefaultOn?}";
        private CastBuilder _castFunctionBuilder = new CastBuilder();

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new Cast(source, _castFunctionBuilder.Build(source, context));
        }
    }

    public class Cast : XTableWrapper
    {
        private int _sourceColumnIndex;
        private IXColumn _castedColumn;
        private List<ColumnDetails> _columns;

        public Cast(IXTable source, IXColumn castedColumn) : base(source)
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

        public override Func<XArray> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _sourceColumnIndex) return _source.ColumnGetter(columnIndex);

            // Pass through the cast for conversions
            return _castedColumn.Getter();
        }
    }
}
