// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using XForm.Aggregators;
using XForm.Data;
using XForm.IO;
using XForm.Transforms;

namespace XForm.Query
{
    internal class ReadCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "read" };
        public string Usage => "'read' [tableNameOrFilePath]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");
            string filePath = parser.NextTableName();
            if (filePath.EndsWith("xform"))
            {
                return new BinaryTableReader(filePath);
            }
            else
            {
                return new TabularFileReader(filePath);
            }
        }
    }

    internal class WriterCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "write" };
        public string Usage => "'write' [tableNameOrFilePath]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            string filePath = parser.NextTableName();
            if (filePath.EndsWith("xform"))
            {
                return new BinaryTableWriter(source, filePath);
            }
            else
            {
                return new TabularFileWriter(source, filePath);
            }
        }
    }

    internal class SchemaCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "schema" };
        public string Usage => "'schema'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            return new SchemaTransformer(source);
        }
    }

    internal class CountCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "count" };
        public string Usage => "'count'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            return new CountAggregator(source);
        }
    }

    internal class MemoryCacheBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "cache" };
        public string Usage => "'cache'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            IDataBatchList sourceList = source as IDataBatchList;
            if (sourceList == null) throw new ArgumentException("'cache' can only be used on IDataBatchList sources.");

            return new MemoryCacher(sourceList);
        }
    }

    internal class LimitCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "limit", "top" };
        public string Usage => "'limit' [RowCount]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            int limit = parser.NextInteger();
            return new RowLimiter(source, limit);
        }
    }

    internal class ColumnsCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "columns", "select" };
        public string Usage => "'columns' [ColumnName], [ColumnName], ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            List<string> columnNames = new List<string>();
            while(!parser.IsLastLinePart)
            {
                columnNames.Add(parser.NextColumnName(source));
            }

            return new ColumnSelector(source, columnNames);
        }
    }

    internal class RemoveColumnsCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "removecolumns" };
        public string Usage => "'removeColumns' [ColumnName], [ColumnName], ...";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            List<string> columnNames = new List<string>();
            while (!parser.IsLastLinePart)
            {
                columnNames.Add(parser.NextColumnName(source));
            }

            return new ColumnRemover(source, columnNames);
        }
    }

    internal class TypeConverterCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "cast", "convert" };
        public string Usage => "'cast' [columnName] [targetType] [default?] [strict?]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            return new TypeConverter(source, 
                parser.NextColumnName(source), 
                parser.NextType(), 
                (parser.IsLastLinePart ? null : parser.NextLiteralValue()), 
                (parser.IsLastLinePart ? false : parser.NextBoolean())
            );
        }
    }

    internal class WhereCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "where" };
        public string Usage => "'where' [columnName] [operator] [value]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            return new WhereFilter(source, 
                parser.NextColumnName(source),
                parser.NextCompareOperator(),
                parser.NextLiteralValue()
            );
        }
    }
}
