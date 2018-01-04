// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace XForm
{
    public class InterfaceLoader
    {
        public const string XFormExtensionPrefix = "XFormExtension";

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
            if (assembly == null)
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

            foreach (Type type in assembly.GetTypes().Where((type) => typeof(T).IsAssignableFrom(type) && !type.IsAbstract && !type.ContainsGenericParameters))
            {
                yield return (T)Build<T>(type);
            }
        }

        public static IEnumerable<T> BuildAll<T>()
        {
            // Load everything in the immediate calling assembly (XForm)
            foreach (T value in BuildAllInAssembly<T>(Assembly.GetCallingAssembly()))
            {
                yield return value;
            }

            // Load everything in assemblies referenced in app.config
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith(XFormExtensionPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string assemblyName = ConfigurationManager.AppSettings[key];

                    foreach (T value in BuildAllInAssembly<T>(Assembly.Load(assemblyName)))
                    {
                        yield return value;
                    }
                }
            }
        }
    }
}
