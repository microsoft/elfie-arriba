// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Arriba.Server
{
    public class Configuration
    {
        protected const string DefaultSource = "Default";
        protected const string RuntimeSource = "Runtime";
        protected const string CommandLineSource = "Command Line";
        protected const string ConfigSource = "Config";

        private const int MaximumConfigSources = 20;
        private const int MaximumSymbolNesting = 10;

        protected List<string> ConfigSources;
        protected List<Dictionary<string, string>> ConfigSettings;

        /// <summary>
        /// Factory method that parses and returns an instance of the config class
        /// </summary>
        /// <param name="args">command line arguments</param>
        /// <returns>config class</returns>
        public static Configuration GetConfigurationForArgs(string[] args)
        {
            return new Configuration(args);
        }

        /// <summary>
        /// Creates an instance of the config class by parsing the arguments and any chained config files
        /// </summary>
        /// <param name="args">command line arguments</param>
        protected Configuration(string[] args)
        {
            ConfigSources = new List<string>();
            ConfigSettings = new List<Dictionary<string, string>>();

            // Add a dictionary for runtime settings (first position)
            Dictionary<string, string> runtimeSettings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            AddSettingsToCollection(RuntimeSource, runtimeSettings);

            // Add Command Line arguments to settings (second position)
            Dictionary<string, string> commandLineSettings = ParseArguments(args);
            AddSettingsToCollection(CommandLineSource, commandLineSettings);

            // Add Default app.config if no config specified in arguments
            if (!InCollection(ConfigSource, commandLineSettings.Keys))
            {
                AddSettingsToCollection("App.Config", OpenConfigForName(String.Empty));
            }
        }

        /// <summary>
        /// Adds a parsed set of settings into the overall collection as well as any descendant configs that are referenced
        /// </summary>
        /// <param name="source">config source name</param>
        /// <param name="settings">parsed config settings</param>
        protected void AddSettingsToCollection(string source, Dictionary<string, string> settings)
        {
            // If there are too many sources, throw
            if (ConfigSettings.Count > MaximumConfigSources) throw new InvalidOperationException(String.Format("Configuration does not support more than {0} sources. Verify a source isn't referencing itself. Sources: [{1}]", MaximumConfigSources, String.Join(", ", ConfigSources.ToArray())));

            // Add this batch of settings to our list
            ConfigSources.Add(source);
            ConfigSettings.Add(settings);

            // Next, if this config references another config, add it also [afterward, so these setting values will override ones from a base config]
            if (InCollection(ConfigSource, settings.Keys))
            {
                string nestedSource = settings[ConfigSource];
                if (!ConfigSources.Contains(nestedSource))
                {
                    Dictionary<string, string> nestedSettings = OpenConfigForName(nestedSource);
                    AddSettingsToCollection(nestedSource, nestedSettings);
                }
            }
        }

        /// <summary>
        /// Parses command line arguments and adds each setting to the colllection one-by-one
        /// </summary>
        /// <remarks>
        /// Command line arguments come in the from of:  /key:value
        /// It's possible for value to have spaces and in that case the whole thing should be wrapped in quotes "/key:value"
        /// </remarks>
        /// <param name="args">command line arguments</param>
        /// <returns>Dictionary populated with a mapping of keys to values</returns>
        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            if (args == null) throw new ArgumentNullException("args");

            StringBuilder errors = new StringBuilder();
            Dictionary<string, string> arguments = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                // Require /name:value optionally wrapped in quotes
                Match m = Regex.Match(arg, "^/([^:]+):(.*?)$");

                if (m.Success == false)
                {
                    // Special Case: If first argument is just a file name and it exists, assume it's a config.
                    if (i == 0)
                    {
                        if (File.Exists(args[0]))
                        {
                            arguments.Add(ConfigSource, args[0]);
                            continue;
                        }
                    }

                    // Otherwise, report an error for this argument
                    errors.AppendLine(String.Format("Error: \"{0}\" didn't match expected pattern. No settings were added to collection.", arg));
                }
                else
                {
                    arguments.Add(m.Groups[1].Value, m.Groups[2].Value.Trim('\"'));
                }
            }

            if (errors.Length > 0) throw new ArgumentException("Error - Command line arguments passed to ParseArguments didn't follow the required format. All arguments should be formatted like: \"/name:value\" (Quotes are optional if the name and value contain no spaces). Errors:\n" + errors.ToString());
            return arguments;
        }

        /// <summary>
        /// Opens and parses an xml config file for settings.
        /// </summary>
        /// <remarks>
        /// File format is similar to the following which mimics standard .NET app.config.
        /// Replacements are possible using the replacement syntax as below.
        /// <configuration>
        ///     <appSettings>
        ///         <add key="key" value="value"/>
        ///         <add key="replacement" value="replacementValue"/>
        ///         <add key="usesReplacement" value="{{replacement}} value with replacement"/>
        ///     </appSettings>
        /// </configuration>
        /// </remarks>
        /// <param name="name">name of the config file to open or empty string for default config name</param>
        /// <returns>parsed set of symbols</returns>
        private static Dictionary<string, string> OpenConfigForName(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            System.Configuration.Configuration config;

            if (name.Length == 0)
            {
                // No name - open default App.Config file name for this exe
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            else
            {
                // Name passed - load config for the given name
                ExeConfigurationFileMap map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = name;
                config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            }

            Dictionary<string, string> settings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string key in config.AppSettings.Settings.AllKeys)
                settings.Add(key, config.AppSettings.Settings[key].Value);

            return settings;
        }

        /// <summary>
        /// Helper that finds if an element exists in the collection
        /// </summary>
        /// <param name="value">value to find</param>
        /// <param name="collection">collection to search</param>
        /// <returns>true if value exists in the collection otherwise false</returns>
        protected static bool InCollection(string value, IEnumerable<string> collection)
        {
            foreach (string item in collection)
            {
                if (String.Compare(value, item, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the value and source of a config option based on the precedence of the config sets
        /// </summary>
        /// <remarks>
        /// Configuration.cs imposes a hierarchy of config settings so that it is possible for nested config settings
        /// to override parent settings and for the command line or programmatically assigned settings to override
        /// config settings.  This function honors that hierarchy during the retreival.
        /// </remarks>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultString">default value if the option is missing</param>
        /// <param name="source">[OUT] returns the source collection for debugging purposes</param>
        /// <returns>The highest precedence setting for a config option</returns>
        private string ResolveConfigurationKey(string keyName, string defaultString, out string source)
        {
            // Look at each settings location in order. The first one with the setting provides the value.
            // They're sorted in priority order, descending.
            for (int i = 0; i < ConfigSettings.Count; ++i)
            {
                if (InCollection(keyName, ConfigSettings[i].Keys))
                {
                    source = ConfigSources[i];
                    return ConfigSettings[i][keyName];
                }
            }

            // *) Fall back to default (return null)
            source = DefaultSource;
            return defaultString;
        }

        /// <summary>
        /// Retreives the pre-parsed value for a config option
        /// </summary>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultString">default value if the option is missing</param>
        /// <returns>The unrolled value for a config setting</returns>
        private string GetConfigurationValue(string keyName, string defaultString)
        {
            return GetConfigurationValue(keyName, defaultString, 0);
        }

        /// <summary>
        /// Retreives the pre-parsed value for a config option, returning the default string if it is missing.  Any replacements in the token
        /// will be unpacked from this call
        /// </summary>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultString">default value if the option is missing</param>
        /// <param name="depth">current unroll depth, used to restrict recursion depth</param>
        /// <returns>The unrolled value for a config setting</returns>
        private string GetConfigurationValue(string keyName, string defaultString, int depth)
        {
            if (depth > MaximumSymbolNesting) throw new InvalidOperationException(String.Format("GetConfigurationValue doesn't support symbols nested more than {0} deep. Ensure you don't have symbols referencing each other circularly.", MaximumSymbolNesting));

            // Get the value for this key, or use the default
            string source = String.Empty;
            string value = ResolveConfigurationKey(keyName, defaultString, out source);
            Debug.WriteLine(String.Format("{3}Configuration[\"{0}\"] = \"{1}\" ({2})", keyName, value, source, new string(' ', depth * 2)));

            // Now, *if the setting was found*, look for symbols in the value. Symbols look like {{SettingName}}.
            if (value != defaultString)
            {
                MatchCollection matches = Regex.Matches(value, @"\{\{([^\{\}]+)\}\}");
                foreach (Match m in matches)
                {
                    // Lookup the other symbol and replace the symbol with the value
                    // If the symbol isn't defined, leave it in as-is.
                    string innerSetting = m.Groups[1].Value;
                    string innerValue = GetConfigurationValue(innerSetting, m.Value, depth + 1);
                    value = value.Replace(m.Value, innerValue);
                }

                // If we replaced values, also log the final value of the setting
                if (matches != null && matches.Count > 0) Trace.WriteLine(String.Format("{3}Configuration[\"{0}\"] [final] = \"{1}\" ({2})", keyName, value, source, new string(' ', depth * 2)));
            }

            return value;
        }

        /// <summary>
        /// Adds or overrides a setting in the config dictionary.  Settings set from code (this API) have higher precedence 
        /// than any settings coming from the cmd line or config files
        /// </summary>
        /// <param name="name">config option name</param>
        /// <param name="value">config option value</param>
        public void SetSetting(string name, string value)
        {
            // Add or set the setting in the first collection, the runtime settings collection
            ConfigSettings[0][name] = value;
        }

        /// <summary>
        /// Returns a configuration parameter as a string
        /// </summary>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultValue">default value if the key is not present or not parsable</param>
        /// <returns>String value for the config option or the default value</returns>
        public string GetConfigurationString(string keyName, string defaultValue)
        {
            return GetConfigurationValue(keyName, defaultValue);
        }

        /// <summary>
        /// Returns a configuration parameter as an integer
        /// </summary>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultValue">default value if the key is not present or not parsable</param>
        /// <returns>Integer value for the config option or the default value</returns>
        public int GetConfigurationInt(string keyName, int defaultValue)
        {
            string value = GetConfigurationValue(keyName, defaultValue.ToString());
            int numericValue;
            if (int.TryParse(value, out numericValue))
                return numericValue;
            else
            {
                Debug.WriteLine(String.Format("Configuration value was not an integer as expected. Falling back to default, {0}.", defaultValue));
                return defaultValue;
            }
        }

        /// <summary>
        /// Returns a configuration parameter as a boolean
        /// </summary>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultValue">default value if the key is not present or not parsable</param>
        /// <returns>Boolean value for the config option or the default value</returns>
        public bool GetConfigurationBool(string keyName, bool defaultValue)
        {
            string value = GetConfigurationValue(keyName, defaultValue.ToString());
            bool boolValue;
            if (bool.TryParse(value, out boolValue))
                return boolValue;
            else
            {
                Debug.WriteLine(String.Format("Configuration value was not a boolean as expected. Falling back to default, {0}.", defaultValue));
                return defaultValue;
            }
        }

        /// <summary>
        /// Returns a configuration parameter as mapped to an enum
        /// </summary>
        /// <typeparam name="T">enum type</typeparam>
        /// <param name="keyName">config option name</param>
        /// <param name="defaultValue">default value if key is not present or not parsable</param>
        /// <returns>Enum value for the config option or the default value</returns>
        public T GetConfigurationEnum<T>(string keyName, T defaultValue)
        {
            string value = GetConfigurationValue(keyName, defaultValue.ToString());
            Type enumType = typeof(T);

            if (!enumType.IsEnum)
                throw new ArgumentException(string.Format("Invalid type '{0}' specified. Type must be an enum", enumType.Name));

            T enumValue = defaultValue;

            try
            {
                enumValue = (T)Enum.Parse(enumType, value, true);
            }
            catch (ArgumentException)
            {
                string expectedList = string.Join(" ", Enum.GetNames(typeof(T)));
                Debug.WriteLine(String.Format("Configuration value was not found in the list of expected values. Falling back to default, {0}.  Expected values are: {1}", defaultValue.ToString(), expectedList));
                enumValue = defaultValue;
            }

            return enumValue;
        }

        /// <summary>
        /// Dumps the config class and known settings.  Helpful for debugging.
        /// </summary>
        public override string ToString()
        {
            StringBuilder content = new StringBuilder();

            for (int i = 0; i < ConfigSettings.Count; ++i)
            {
                content.AppendLine(ConfigSources[i] + " Settings:");
                foreach (string key in ConfigSettings[i].Keys)
                    content.AppendLine(String.Format("\t[{0}]: [{1}]", key, ConfigSettings[i][key]));
                content.AppendLine();
            }

            return content.ToString();
        }
    }
}

