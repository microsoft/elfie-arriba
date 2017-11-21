using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace XForm.Extensions
{
    public class InterfaceLoader
    {
        public static T Build<T>(Type type)
        {
            ConstructorInfo ctor = type.GetConstructor(Array.Empty<Type>());
            if (ctor == null)
            {
                Trace.WriteLine($"ProviderLoader could not build requested type \"{type.Name}\" from \"{type.Assembly.Location}\". Type did not have an empty constructor.");
            }

            return (T)(ctor.Invoke(null));
        }

        public static T Build<T>(string typeName, string assemblyName)
        {
            Assembly assembly = Assembly.Load(assemblyName);
            if(assembly == null)
            {
                Trace.WriteLine($"ProviderLoader could not build requested type \"{typeName}\" from \"{assemblyName}\". Assembly was not found or could not be loaded.");
                return default(T);
            }

            Type type = assembly.GetType(typeName);
            if (type == null)
            {
                Trace.WriteLine($"ProviderLoader could not build requested type \"{typeName}\" from \"{assembly.Location}\". Type was not found.");
                return default(T);
            }

            return Build<T>(type);
        }

        /// <summary>
        ///  Build an instance of every type in a given assembly which is the given base type or
        ///  implements the given base interface, using the empty constructors.
        /// </summary>
        /// <typeparam name="T">Interface or Base Class Type to construct instances of</typeparam>
        /// <param name="assembly">Assembly from which to find all matching types</param>
        /// <returns>An instance of each matching type in Assembly built by the empty constructor.</returns>
        private static IEnumerable<T> BuildAllInAssembly<T>(Assembly assembly = null)
        {
            if (assembly == null) assembly = Assembly.GetCallingAssembly();

            foreach(Type type in assembly.GetTypes().Where((type) => typeof(T).IsAssignableFrom(type) && !type.IsAbstract && !type.ContainsGenericParameters))
            {
                yield return (T)Build<T>(type);
            }
        }

        /// <summary>
        ///  Build an instance of every type listed in a given app.config for a given base type or
        ///  interface type, using the empty constructors.
        /// </summary>
        /// <typeparam name="T">Interface or Base Class Type to construct interfaces of</typeparam>
        /// <returns>An instance of each type implementing T from app.config</returns>
        private static IEnumerable<T> BuildAllFromConfig<T>()
        {
            string interfaceName = typeof(T).Name;

            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith(interfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    T instance = Build<T>(settings[1], settings[0]);
                    if (instance != null) yield return instance;
                }
            }
        }

        public static IEnumerable<T> BuildAll<T>()
        {
            foreach(T value in BuildAllInAssembly<T>(Assembly.GetCallingAssembly()))
            {
                yield return value;
            }

            foreach(T value in BuildAllFromConfig<T>())
            {
                yield return value;
            }
        }
    }
}
