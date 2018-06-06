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
            // Cast can be evaluated in parallel, so keep parallel
            return source.WrapParallel(context.Parser, (part) => new Cast(part, _castFunctionBuilder.Build(part, context)));
        }
    }

    public class Cast : XTableWrapper
    {
        private int _sourceColumnIndex;
        private IXColumn _castedColumn;
        private List<IXColumn> _columns;

        public Cast(IXTable source, IXColumn castedColumn) : base(source)
        {
            _sourceColumnIndex = source.Columns.IndexOfColumn(castedColumn.ColumnDetails.Name);
            _castedColumn = castedColumn;

            _columns = new List<IXColumn>();
            for (int i = 0; i < source.Columns.Count; ++i)
            {
                _columns.Add((i == _sourceColumnIndex ? castedColumn : source.Columns[i]));
            }
        }

        public override IReadOnlyList<IXColumn> Columns => _columns;
    }
}
