// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Types;

namespace XForm.IO
{
    public class TabularFileWriter : IXTable
    {
        private IXTable _source;
        private IStreamProvider _streamProvider;
        private string _outputFilePath;
        private ITabularWriter _writer;

        private Func<XArray>[] _stringColumnGetters;

        public TabularFileWriter(IXTable source, IStreamProvider streamProvider, string outputFilePath)
        {
            _source = source;
            _streamProvider = streamProvider;
            _outputFilePath = outputFilePath;
            Initialize();
        }

        public TabularFileWriter(IXTable source, ITabularWriter writer)
        {
            _source = source;
            _writer = writer;
            _writer.SetColumns(_source.Columns.Select((col) => col.ColumnDetails.Name));

            Initialize();
        }

        private void Initialize()
        {
            // Subscribe to all columns and cache converters for them
            _stringColumnGetters = new Func<XArray>[_source.Columns.Count];
            for (int i = 0; i < _source.Columns.Count; ++i)
            {
                Func<XArray> rawGetter = _source.Columns[i].CurrentGetter();
                Func<XArray> stringGetter = rawGetter;

                if (_source.Columns[i].ColumnDetails.Type != typeof(String8))
                {
                    Func<XArray, XArray> converter = TypeConverterFactory.GetConverter(_source.Columns[i].ColumnDetails.Type, typeof(String8));
                    stringGetter = () => (converter(rawGetter()));
                }

                _stringColumnGetters[i] = stringGetter;
            }
        }

        public IReadOnlyList<IXColumn> Columns => _source.Columns;
        public int CurrentRowCount => _source.CurrentRowCount;

        public void Reset()
        {
            _source.Reset();

            // If this is a reset, ensure the old writer is Disposed (and flushes output)
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;

                // On Dispose, tell the StreamProvider to publish the table
                _streamProvider.Publish(_outputFilePath);
            }
        }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Build the writer only when we start getting rows
            if (_writer == null)
            {
                if (_outputFilePath == null) throw new InvalidOperationException("TabularFileWriter can't reset when passed an ITabularWriter instance");
                if (_outputFilePath.Equals("cout", StringComparison.OrdinalIgnoreCase))
                {
                    _writer = new ConsoleTabularWriter();
                }
                else
                {
                    _writer = TabularFactory.BuildWriter(_streamProvider.OpenWrite(_outputFilePath), _outputFilePath);
                }

                _writer.SetColumns(_source.Columns.Select((col) => col.ColumnDetails.Name));
            }

            // Or smaller batch?
            int rowCount = _source.Next(desiredCount, cancellationToken);
            if (rowCount == 0) return 0;

            XArray[] arrays = new XArray[_stringColumnGetters.Length];
            for (int i = 0; i < _stringColumnGetters.Length; ++i)
            {
                arrays[i] = _stringColumnGetters[i]();
            }

            for (int rowIndex = 0; rowIndex < rowCount; ++rowIndex)
            {
                for (int colIndex = 0; colIndex < _stringColumnGetters.Length; ++colIndex)
                {
                    String8 value = ((String8[])arrays[colIndex].Array)[arrays[colIndex].Index(rowIndex)];
                    _writer.Write(value);
                }

                _writer.NextRow();
            }

            return rowCount;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            if (_writer != null)
            {
                try
                {
                    _writer.Dispose();

                    // On Dispose, tell the StreamProvider to publish the table
                    if (_streamProvider != null) _streamProvider.Publish(_outputFilePath);
                }
                finally
                {
                    _writer = null;
                }
            }
        }
    }
}
