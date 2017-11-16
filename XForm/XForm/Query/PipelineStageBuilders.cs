// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Aggregators;
using XForm.Data;
using XForm.IO;
using XForm.Transforms;
using XForm.Types;

namespace XForm.Query
{
    public interface IPipelineStageBuilder
    {
        IEnumerable<string> Verbs { get; }
        IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts);
    }

    internal class ReadCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "read" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");
            if (configurationParts.Count != 2) throw new ArgumentException($"Usage: 'read' [filePath]");
            if (configurationParts[1].EndsWith("xform"))
            {
                return new BinaryTableReader(configurationParts[1]);
            }
            else
            {
                return new TabularFileReader(configurationParts[1]);
            }
        }
    }

    internal class SchemaCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "schema" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (configurationParts.Count != 1) throw new ArgumentException("Usage: 'schema'");
            return new SchemaTransformer(source);
        }
    }

    internal class ColumnsCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "columns", "select" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            return new ColumnSelector(source, configurationParts.Skip(1));
        }
    }

    internal class RemoveColumnsCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "removecolumns" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            return new ColumnRemover(source, configurationParts.Skip(1));
        }
    }

    internal class WriterCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "write" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (configurationParts.Count != 2) throw new ArgumentException("Usage 'write' [filePath]");
            if (configurationParts[1].EndsWith("xform"))
            {
                return new BinaryTableWriter(source, configurationParts[1]);
            }
            else
            {
                return new TabularFileWriter(source, configurationParts[1]);
            }
        }
    }

    internal class LimitCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "limit", "top"};

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            int limit;
            if (configurationParts.Count != 2 || !int.TryParse(configurationParts[1], out limit)) throw new ArgumentException("Usage: 'limit' [rowCount]");
            return new RowLimiter(source, limit);
        }
    }

    internal class CountCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "count" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (configurationParts.Count != 1) throw new ArgumentException("Usage: 'count'");
            return new CountAggregator(source);
        }
    }

    internal class TypeConverterCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "cast", "convert" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (configurationParts.Count < 3 || configurationParts.Count > 5) throw new ArgumentException("Usage: 'cast' [columnName] [targetType] [default?] [strict?]");
            return new TypeConverter(source, configurationParts[1], TypeProviderFactory.Get(configurationParts[2]).Type, (configurationParts.Count > 3 ? configurationParts[2] : null), (configurationParts.Count > 4 ? bool.Parse(configurationParts[3]) : true));
        }
    }

    internal class WhereCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "where" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (configurationParts.Count != 4) throw new ArgumentException("Usage: 'where' [columnName] [operator] [value]");
            return new WhereFilter(source, configurationParts[1], configurationParts[2].ParseCompareOperator(), configurationParts[3]);
        }
    }

    internal class MemoryCacheBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "cache" };

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, List<string> configurationParts)
        {
            if (configurationParts.Count != 1) throw new ArgumentException("Usage: 'cache'");

            IDataBatchList sourceList = source as IDataBatchList;
            if (sourceList == null) throw new ArgumentException("'cache' can only be used on IDataBatchList sources.");

            return new MemoryCacher(sourceList);
        }
    }
}
