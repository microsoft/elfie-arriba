using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XForm.CST
{
    public class SarifWriter : ITabularWriter
    {
        private Stream _outputStream;

        private List<string> _columns;
        private Action<object>[] _columnWriters;

        private SarifLog _log;
        private Run _run;

        private Result _currentRow;
        private PhysicalLocation _currentLocation;

        private int _columnsWritten;


        private bool _inPartialColumn;
        private StringBuilder _currentPartialValue;
        
        public int RowCountWritten { get; private set; }
        public long BytesWritten { get; private set; }

        public SarifWriter(string filePath) : this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        { }

        public SarifWriter(Stream output)
        {
            _outputStream = output;

            // Create a basic SarifLog, Tool, and Run to write out
            _run = new Run();
            _run.Tool = new Tool() { Name = "XForm" };
            _run.Results = new List<Result>();

            _log = new SarifLog();
            _log.Version = SarifVersion.OneZeroZero;

            _log.Runs = new List<Run>();
            _log.Runs.Add(_run);

            // Create a Result instance for the first row
            _currentRow = new Result();
        }

        public void SetColumns(IEnumerable<string> columnNames)
        {
            // Store the column names available
            _columns = new List<string>(columnNames);

            // Pre-Build a hard-coded writer method for each column name
            _columnWriters = columnNames.Select((name) => WriterForColumn(name)).ToArray();
        }

        private Action<object> WriterForColumn(string columnName)
        {
            switch (columnName)
            {
                case "RuleId":
                    return (value) => _currentRow.RuleId = value.ToString();
                case "Message":
                    return (value) => _currentRow.Message = value.ToString();
                case "ToolFingerprintContribution":
                    return (value) => _currentRow.ToolFingerprintContribution = value.ToString();
                case "Location.Uri":
                    return (value) =>
                    {
                        InitializeLocation();
                        _currentLocation.Uri = new Uri(value.ToString(), UriKind.RelativeOrAbsolute);
                    };
                case "Location.StartLine":
                    return (value) =>
                    {
                        InitializeLocation();
                        _currentLocation.Region.StartLine = AsInt(value);
                    };
                case "Location.StartColumn":
                    return (value) =>
                    {
                        InitializeLocation();
                        _currentLocation.Region.StartColumn = AsInt(value);
                    };
            }

            if (columnName.StartsWith("Properties."))
            {
                string propertyName = columnName.Substring("Properties.".Length);
                return (value) => _currentRow.SetProperty<string>(propertyName, value.ToString());
            }

            return Nothing;
        }

        private void Nothing(object value)
        { }

        private void InitializeLocation()
        {
            if(_currentLocation == null)
            {
                _currentLocation = new PhysicalLocation();
                _currentLocation.Region = new Region();

                _currentRow.Locations = new List<Location>();
                _currentRow.Locations.Add(new Location() { ResultFile = _currentLocation });
            }
        }

        private int AsInt(object value)
        {
            if (value == null) return 0;
            if (value is int) return (int)value;
            return int.Parse(value.ToString());
        }

        public void Write(String8 value)
        {
            _columnWriters[_columnsWritten](value);
            _columnsWritten++;
        }

        public void Write(DateTime value)
        {
            _columnWriters[_columnsWritten](value);
            _columnsWritten++;
        }

        public void Write(int value)
        {
            _columnWriters[_columnsWritten](value);
            _columnsWritten++;
        }

        public void Write(bool value)
        {
            _columnWriters[_columnsWritten](value);
            _columnsWritten++;
        }

        public void Write(byte value)
        {
            _columnWriters[_columnsWritten](value);
            _columnsWritten++;
        }

        public void WriteValueEnd()
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValueEnd.");

            _columnWriters[_columnsWritten](_currentPartialValue.ToString());
            _columnsWritten++;

            _inPartialColumn = false;
            _currentPartialValue.Clear();
        }

        public void WriteValuePart(String8 part)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            _currentPartialValue.Append(part.ToString());
        }

        public void WriteValuePart(DateTime value)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            _currentPartialValue.Append(value.ToString("u"));
        }

        public void WriteValuePart(int part)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            _currentPartialValue.Append(part);
        }

        public void WriteValuePart(bool part)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            _currentPartialValue.Append(part);
        }

        public void WriteValuePart(byte c)
        {
            if (!_inPartialColumn) throw new InvalidOperationException("WriteValueStart must be called before WriteValuePart.");
            _currentPartialValue.Append((char)c);
        }

        public void WriteValueStart()
        {
            if (_currentPartialValue == null) _currentPartialValue = new StringBuilder();
            _inPartialColumn = true;
        }

        public void NextRow()
        {
            this.RowCountWritten++;

            _columnsWritten = 0;

            _run.Results.Add(_currentRow);
            _currentRow = new Result();
            _currentLocation = null;
        }

        public void Dispose()
        {
            // Write the SarifLog to the OutputStream using the Json.net JsonSerializer
            if(_outputStream != null)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    ContractResolver = SarifContractResolver.Instance,
                    Formatting = Formatting.Indented
                };

                // Write the log (and dispose the stream)
                using (StreamWriter sw = new StreamWriter(_outputStream))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    JsonSerializer serializer = JsonSerializer.Create(settings);
                    serializer.Serialize(jw, _log);
                }

                _outputStream = null;
            }
        }
    }
}
