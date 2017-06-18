// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace Xsv
{
    public static class AppConfigExtensibility
    {
        /// <summary>
        ///  Add all instances of a given interface specified in app.config keys to a dictionary.
        ///  In app.config, add keys like:
        ///   key="InterfaceName.Anything" value="dictionary,keys,to,register;AssemblyNameWithoutExtension;NamespaceAndTypeName"
        /// </summary>
        /// <typeparam name="T">Interface or Base Class type to find extensions to</typeparam>
        /// <param name="implementations">Dictionary of name to instance to add extensions to</param>
        public static void AddExtensionsOf<T>(Dictionary<string, T> implementations) where T : class
        {
            string appConfigPrefix = typeof(T).Name;

            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key.StartsWith(appConfigPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string[] settings = ConfigurationManager.AppSettings[key].Split(';');
                    T instance = ConstructWithEmptyConstructor<T>(key, settings[1], settings[2]);
                    if (instance == null) continue;

                    foreach (string name in settings[0].Split(','))
                    {
                        implementations[name] = instance;
                    }
                }
            }
        }

        /// <summary>
        ///  Dynamically load a given type from a given assembly and construct an instance using the empty constructor.
        /// </summary>
        /// <typeparam name="T">Type of item to construct (interface or base type)</typeparam>
        /// <param name="keyName">Key name in app.config for error messages</param>
        /// <param name="assemblyName">Assembly Path and name in which type is defined</param>
        /// <param name="typeName">Namespace and type name type in assembly</param>
        /// <returns>Instance of typeName or null if it couldn't be found, constructed, or cast</returns>
        public static T ConstructWithEmptyConstructor<T>(string keyName, string assemblyName, string typeName) where T : class
        {
            Assembly asm = Assembly.Load(assemblyName);
            Type readerType = asm.GetType(typeName);
            if (readerType == null)
            {
                Trace.WriteLine($"Could not add {typeof(T).Name} extension \"{keyName}\". Type \"{typeName}\" not found in assembly \"{assemblyName}\".");
                return default(T);
            }

            ConstructorInfo ctor = readerType.GetConstructor(new Type[0]);
            if (ctor == null)
            {
                Trace.WriteLine($"Could not add {typeof(T).Name} extension \"{keyName}\". Type \"{typeName}\" in assembly \"{assemblyName}\" had no empty constructor.");
                return default(T);
            }

            return ctor.Invoke(new object[0]) as T;
        }
    }
}
