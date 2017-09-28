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
        private static Dictionary<string, Func<string, ITabularReader>> s_readers;
        private static Dictionary<string, Func<string, ITabularWriter>> s_writers;

        private static void LoadReadersAndWriters()
        {
            s_readers = new Dictionary<string, Func<string, ITabularReader>>(StringComparer.OrdinalIgnoreCase);
            s_writers = new Dictionary<string, Func<string, ITabularWriter>>(StringComparer.OrdinalIgnoreCase);

            s_readers["csv"] = (path) => new CsvReader(path);
            s_readers["csvNH"] = (path) => new CsvReader(MapExtension(path, ".csv"), false);
            s_readers["tsv"] = (path) => new TsvReader(path);
            s_readers["tsvNH"] = (path) => new TsvReader(MapExtension(path, ".tsv"), false);
            s_readers["iislog"] = (path) => new IISTabularReader(MapExtension(path, ".log"));
            s_readers["ldf"] = (path) => new LdfTabularReader(path);
            s_readers["ldif"] = (path) => new LdfTabularReader(path);

            s_writers["cout"] = (path) => new ConsoleTabularWriter();
            s_writers["csv"] = (path) => new CsvWriter(path, true);
            s_writers["tsv"] = (path) => new TsvWriter(path, true);
            s_writers["json"] = (path) => new JsonTabularWriter(path);

            // Register ITabularReader and ITabularWriter ctors from app.config
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith("ITabularReader", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    Func<string, ITabularReader> ctor = GetStringConstructorFunc<ITabularReader>(key, settings[1], settings[2]);
                    if (ctor == null) continue;

                    foreach (string extension in settings[0].Split(','))
                    {
                        s_readers[extension] = ctor;
                    }
                }
                else if (key.StartsWith("ITabularWriter", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    Func<string, ITabularWriter> ctor = GetStringConstructorFunc<ITabularWriter>(key, settings[1], settings[2]);
                    if (ctor == null) continue;

                    foreach (string extension in settings[0].Split(','))
                    {
                        s_writers[extension] = ctor;
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

        private static Func<string, T> GetStringConstructorFunc<T>(string keyName, string assemblyName, string typeName)
        {
            Assembly asm = Assembly.Load(assemblyName);
            Type readerType = asm.GetType(typeName);
            if (readerType == null)
            {
                Trace.WriteLine(String.Format("TabularFactory could not add \"{0}\". Type \"{1}\" not found in assembly \"{2}\".", keyName, typeName, assemblyName));
                return null;
            }

            ConstructorInfo ctor = readerType.GetConstructor(new Type[] { typeof(string) });
            if (ctor == null)
            {
                Trace.WriteLine(String.Format("TabularFactory could not add \"{0}\". No constructor taking only a string found on Type \"{1}\" in assembly \"{2}\".", keyName, typeName, assemblyName));
                return null;
            }

            ParameterExpression stringParameter = Expression.Parameter(typeof(string), "filePath");
            return Expression.Lambda<Func<string, T>>(Expression.New(ctor, stringParameter), stringParameter).Compile();
        }

        public static ITabularReader BuildReader(string filePath)
        {
            if (s_readers == null) LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            Func<string, ITabularReader> ctor;
            if (s_readers.TryGetValue(extension, out ctor)) return ctor(filePath);

            throw new NotSupportedException(String.Format("Xsv does know how to read \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", s_readers.Keys)));
        }

        public static ITabularWriter BuildWriter(string filePath)
        {
            if (s_writers == null) LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            if (String.IsNullOrEmpty(extension)) extension = filePath;

            Func<string, ITabularWriter> ctor;
            if (s_writers.TryGetValue(extension, out ctor)) return ctor(filePath);

            throw new NotSupportedException(String.Format("Xsv does not know how to write \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", s_writers.Keys)));
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
