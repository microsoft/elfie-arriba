// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public static class TabularFactory
    {
        private static Dictionary<string, Func<string, ITabularReader>> s_PathToReader;
        private static Dictionary<string, Func<string, ITabularWriter>> s_PathToWriter;
        private static Dictionary<string, Func<Stream, ITabularReader>> s_StreamToReader;
        private static Dictionary<string, Func<Stream, ITabularWriter>> s_StreamToWriter;

        private static void LoadReadersAndWriters()
        {
            // Only load if not already loaded
            if (s_PathToReader != null) return;

            s_PathToReader = new Dictionary<string, Func<string, ITabularReader>>(StringComparer.OrdinalIgnoreCase);
            s_PathToWriter = new Dictionary<string, Func<string, ITabularWriter>>(StringComparer.OrdinalIgnoreCase);
            s_StreamToReader = new Dictionary<string, Func<Stream, ITabularReader>>(StringComparer.OrdinalIgnoreCase);
            s_StreamToWriter = new Dictionary<string, Func<Stream, ITabularWriter>>(StringComparer.OrdinalIgnoreCase);

            s_PathToReader["csv"] = (path) => new CsvReader(path);
            s_PathToReader["csvNH"] = (path) => new CsvReader(MapExtension(path, ".csv"), false);
            s_PathToReader["tsv"] = (path) => new TsvReader(path);
            s_PathToReader["tsvNH"] = (path) => new TsvReader(MapExtension(path, ".tsv"), false);
            s_PathToReader["iislog"] = (path) => new IISTabularReader(MapExtension(path, ".log"));
            s_PathToReader["ldf"] = (path) => new LdfTabularReader(path);
            s_PathToReader["ldif"] = (path) => new LdfTabularReader(path);

            s_PathToWriter["cout"] = (path) => new ConsoleTabularWriter();
            s_PathToWriter["csv"] = (path) => new CsvWriter(path, true);
            s_PathToWriter["tsv"] = (path) => new TsvWriter(path, true);
            s_PathToWriter["json"] = (path) => new JsonTabularWriter(path);

            s_StreamToReader["csv"] = (stream) => new CsvReader(stream);
            s_StreamToReader["tsv"] = (stream) => new TsvReader(stream);
            s_StreamToWriter["csv"] = (stream) => new CsvWriter(stream);
            s_StreamToWriter["tsv"] = (stream) => new TsvWriter(stream);
            s_StreamToWriter["json"] = (stream) => new JsonTabularWriter(stream);

            // Register ITabularReader and ITabularWriter ctors from app.config
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith("ITabularReader", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    Func<string, ITabularReader> stringCtor = GetConstructorFunc<ITabularReader, string>(key, settings[1], settings[2]);
                    Func<Stream, ITabularReader> streamCtor = GetConstructorFunc<ITabularReader, Stream>(key, settings[1], settings[2]);

                    foreach (string extension in settings[0].Split(','))
                    {
                        if (stringCtor != null) s_PathToReader[extension] = stringCtor;
                        if (streamCtor != null) s_StreamToReader[extension] = streamCtor;
                    }
                }
                else if (key.StartsWith("ITabularWriter", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    Func<string, ITabularWriter> stringCtor = GetConstructorFunc<ITabularWriter, string>(key, settings[1], settings[2]);
                    Func<Stream, ITabularWriter> streamCtor = GetConstructorFunc<ITabularWriter, Stream>(key, settings[1], settings[2]);

                    foreach (string extension in settings[0].Split(','))
                    {
                        if (stringCtor != null) s_PathToWriter[extension] = stringCtor;
                        if (streamCtor != null) s_StreamToWriter[extension] = streamCtor;
                    }
                }
            }
        }

        private static string MapExtension(string filePath, string toExtension)
        {
            // If the file exists with that extension, leave it alone
            if (File.Exists(filePath)) return filePath;

            // Otherwise, change the extension
            return Path.ChangeExtension(filePath, toExtension);
        }

        private static Func<U, T> GetConstructorFunc<T, U>(string keyName, string assemblyName, string typeName)
        {
            Assembly asm;
            if (assemblyName.Contains("\\"))
            {
                string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string expectedPath = Path.Combine(exePath, assemblyName + ".dll");
                asm = Assembly.LoadFrom(expectedPath);
            }
            else
            {
                asm = Assembly.Load(assemblyName);
            }

            Type readerType = asm.GetType(typeName);
            if (readerType == null)
            {
                Trace.WriteLine($"TabularFactory could not add \"{keyName}\". Type \"{typeName}\" not found in assembly \"{assemblyName}\".");
                return null;
            }

            ConstructorInfo ctor = readerType.GetConstructor(new Type[] { typeof(U) });
            if (ctor == null)
            {
                Trace.WriteLine($"TabularFactory could not add \"{keyName}\". No constructor taking only a \"{typeof(U).Name}\" found on Type \"{typeName}\" in assembly \"{assemblyName}\".");
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(U), "source");
            return Expression.Lambda<Func<U, T>>(Expression.New(ctor, parameter), parameter).Compile();
        }

        public static ITabularReader BuildReader(string filePath)
        {
            LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            Func<string, ITabularReader> ctor;
            if (s_PathToReader.TryGetValue(extension, out ctor)) return ctor(filePath);

            throw new NotSupportedException(String.Format("Xsv does know how to read \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", s_PathToReader.Keys)));
        }

        public static ITabularReader BuildReader(Stream stream, string filePath)
        {
            LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            Func<Stream, ITabularReader> ctor;
            if (s_StreamToReader.TryGetValue(extension, out ctor)) return ctor(stream);

            throw new NotSupportedException(String.Format("Xsv does know how to read \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", s_StreamToReader.Keys)));
        }

        public static ITabularWriter BuildWriter(string filePath)
        {
            LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            if (String.IsNullOrEmpty(extension)) extension = filePath;

            Func<string, ITabularWriter> ctor;
            if (s_PathToWriter.TryGetValue(extension, out ctor)) return ctor(filePath);

            throw new NotSupportedException(String.Format("Xsv does not know how to write \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", s_PathToWriter.Keys)));
        }

        public static ITabularWriter BuildWriter(Stream stream, string filePath)
        {
            LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            Func<Stream, ITabularWriter> ctor;
            if (s_StreamToWriter.TryGetValue(extension, out ctor)) return ctor(stream);

            throw new NotSupportedException(String.Format("Xsv does know how to write \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", s_StreamToWriter.Keys)));
        }

        public static ITabularWriter AppendWriter(string filePath, IEnumerable<string> columnNames)
        {
            ITabularWriter writer;

            // If the file doesn't exist, make a new writer
            if (!File.Exists(filePath))
            {
                writer = BuildWriter(filePath);
                writer.SetColumns(columnNames);
                return writer;
            }

            // Verify columns match
            string expectedColumns = string.Join(", ", columnNames);

            using (ITabularReader r = TabularFactory.BuildReader(filePath))
            {
                string actualColumns = string.Join(", ", r.Columns);
                if (string.Compare(expectedColumns, actualColumns, true) != 0)
                {
                    throw new InvalidOperationException(string.Format("Can't append to \"{0}\" because the column names don't match.\r\nExpect: {1}\r\nActual: {2}", filePath, expectedColumns, actualColumns));
                }
            }

            // Build the writer
            FileStream s = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            switch (extension)
            {
                case "csv":
                    writer = new CsvWriter(s, false);
                    break;
                case "tsv":
                    writer = new TsvWriter(s, false);
                    break;
                default:
                    s.Dispose();
                    throw new NotSupportedException(String.Format("Xsv does not know how to append to \"{0}\". Known Extensions: [csv, tsv]", extension));
            }

            // Set the columns so the writer knows the count (writers shouldn't write the columns if writeHeaderRow was false)
            writer.SetColumns(columnNames);

            return writer;
        }
    }
}
