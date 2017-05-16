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
        private static Dictionary<string, Func<string, ITabularReader>> Readers;
        private static Dictionary<string, Func<string, ITabularWriter>> Writers;

        private static void LoadReadersAndWriters()
        {
            Readers = new Dictionary<string, Func<string, ITabularReader>>(StringComparer.OrdinalIgnoreCase);
            Writers = new Dictionary<string, Func<string, ITabularWriter>>(StringComparer.OrdinalIgnoreCase);

            Readers["csv"] = (path) => new CsvReader(path);
            Readers["tsv"] = (path) => new TsvReader(path);

            Writers["cout"] = (path) => new ConsoleTabularWriter();
            Writers["csv"] = (path) => new CsvWriter(path, true);
            Writers["tsv"] = (path) => new TsvWriter(path, true);
            Writers["json"] = (path) => new JsonTabularWriter(path);

            // Register ITabularReader and ITabularWriter ctors from app.config
            foreach(string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if(key.StartsWith("ITabularReader", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    Func<string, ITabularReader> ctor = GetStringConstructorFunc<ITabularReader>(key, settings[1], settings[2]);
                    if (ctor == null) continue;

                    foreach (string extension in settings[0].Split(','))
                    {
                        Readers[extension] = ctor;
                    }
                }
                else if(key.StartsWith("ITabularWriter", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    Func<string, ITabularWriter> ctor = GetStringConstructorFunc<ITabularWriter>(key, settings[1], settings[2]);
                    if (ctor == null) continue;

                    foreach (string extension in settings[0].Split(','))
                    {
                        Writers[extension] = ctor;
                    }
                }
            }
        }

        private static Func<string, T> GetStringConstructorFunc<T>(string keyName, string assemblyName, string typeName)
        {
            Assembly asm = Assembly.Load(assemblyName);
            Type readerType = asm.GetType(typeName);
            if(readerType == null)
            {
                Trace.WriteLine(String.Format("TabularFactory could not add \"{0}\". Type \"{1}\" not found in assembly \"{2}\".", keyName, typeName, assemblyName));
                return null;
            }

            ConstructorInfo ctor = readerType.GetConstructor(new Type[] { typeof(string) });
            if(ctor == null)
            {
                Trace.WriteLine(String.Format("TabularFactory could not add \"{0}\". No constructor taking only a string found on Type \"{1}\" in assembly \"{2}\".", keyName, typeName, assemblyName));
                return null;
            }

            ParameterExpression stringParameter = Expression.Parameter(typeof(string), "filePath");
            return Expression.Lambda<Func<string, T>>(Expression.New(ctor, stringParameter), stringParameter).Compile();
        }

        public static ITabularReader BuildReader(string filePath)
        {
            if (Readers == null) LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            Func<string, ITabularReader> ctor;
            if (Readers.TryGetValue(extension, out ctor)) return ctor(filePath);

            throw new NotSupportedException(String.Format("Xsv does know how to read \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", Readers.Keys)));
        }

        public static ITabularWriter BuildWriter(string filePath)
        {
            if (Writers == null) LoadReadersAndWriters();

            string extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
            if (String.IsNullOrEmpty(extension)) extension = filePath;

            Func<string, ITabularWriter> ctor;
            if (Writers.TryGetValue(extension, out ctor)) return ctor(filePath);

            throw new NotSupportedException(String.Format("Xsv does not know how to write \"{0}\". Known Extensions: [{1}]", extension, String.Join(", ", Writers.Keys)));
        }
    }
}
