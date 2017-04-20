using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using XsvConcat;

namespace Xsv.Sanitize
{
    public interface ISanitizerProvider
    {
        /// <summary>
        ///  Get the ISanitizeMapper to transform to a named type of thing (PersonName, IP).
        /// </summary>
        /// <param name="name">Name for which to get mapper</param>
        /// <returns>Mapper for name</returns>
        ISanitizeMapper Mapper(string name);
    }

    public class SanitizerProvider : ISanitizerProvider
    {
        private static Dictionary<string, ISanitizeMapper> Mappers { get; set; }

        static SanitizerProvider()
        {
            LoadMappers();
        }

        private static void LoadMappers()
        {
            Mappers = new Dictionary<string, ISanitizeMapper>(StringComparer.OrdinalIgnoreCase);

            // Register default mappers
            Mappers[string.Empty] = Mappers["Phrase"] = new PhraseMapper();
            Mappers["Alias"] = new AliasMapper();
            Mappers["IP"] = new IpMapper();
            Mappers["PersonName"] = new PersonNameMapper();
            Mappers["ComputerName"] = new ComputerNameMapper();
            Mappers["Int"] = new IntMapper();
            Mappers["Guid"] = new GuidMapper();

            // Register ISanitizeMappers from app.config
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith("ISanitizerMapper", StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    ISanitizeMapper instance = Construct(key, settings[1], settings[2]);
                    if (instance == null) continue;

                    foreach (string name in settings[0].Split(','))
                    {
                        Mappers[name] = instance;
                    }
                }
            }
        }

        private static ISanitizeMapper Construct(string keyName, string assemblyName, string typeName)
        {
            Assembly asm = Assembly.Load(assemblyName);
            Type readerType = asm.GetType(typeName);
            if (readerType == null)
            {
                Trace.WriteLine(String.Format("SanitizerProvider could not add \"{0}\". Type \"{1}\" not found in assembly \"{2}\".", keyName, typeName, assemblyName));
                return null;
            }

            ConstructorInfo ctor = readerType.GetConstructor(new Type[0]);
            if (ctor == null)
            {
                Trace.WriteLine(String.Format("SanitizerProvider could not add \"{0}\". No empty constructor found on Type \"{1}\" in assembly \"{2}\".", keyName, typeName, assemblyName));
                return null;
            }

            return (ISanitizeMapper)ctor.Invoke(new object[0]);
        }

        public ISanitizeMapper Mapper(string name)
        {
            ISanitizeMapper result;
            if (!Mappers.TryGetValue(name, out result)) throw new UsageException($"SanitizerProvider doesn't have a provider for '{name}'.");
            return result;
        }
    }
}
