// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Arriba.Diagnostics
{
    public class CommandLine
    {
        public const string RemoveExecutableExpression = "^([^/ ]+|\"[^/\"]+\")";
        public const string CommandLineExpression = "/(?<name>[^:]+):((?<value>[^ \"]+)|(\"(?<value>([^\"]|\"\")+)\"))";
        private Dictionary<string, string> Values { get; set; }

        private CommandLine(Dictionary<string, string> values)
        {
            this.Values = values;
        }

        /// <summary>
        ///  Parse the Command Line and return a CommandLine instance to get values from.
        /// </summary>
        /// <returns>CommandLine instance from which values can be retrieved</returns>
        public static CommandLine Parse()
        {
            return CommandLine.Parse(Environment.CommandLine);
        }

        /// <summary>
        ///  Parse the provided Command Line and return a CommandLine instance to get values from.
        /// </summary>
        /// <param name="commandLine">Command Line to parse</param>
        /// <returns>CommandLine instance from which values can be retrieved</returns>
        public static CommandLine Parse(string commandLine)
        {
            // Find and remove the executable at the beginning of the command line
            Match exe = Regex.Match(commandLine, RemoveExecutableExpression);
            if (exe.Success) commandLine = commandLine.Substring(exe.Length);

            // Build a Dictionary for each argument found
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match arg in Regex.Matches(commandLine, CommandLineExpression))
            {
                string name = arg.Groups["name"].Value;
                string valueUnescaped = arg.Groups["value"].Value.Replace("\"\"", "\"");
                if (valueUnescaped.Equals("?")) throw new CommandLineUsageException();

                values[name] = valueUnescaped;
            }

            if (values.Count == 0 && commandLine.Length > 0)
            {
                throw new CommandLineMalformedException(commandLine);
            }

            return new CommandLine(values);
        }

        /// <summary>
        ///  Get a string parameter which is required.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter; MissingRequiredArgumentException thrown if not provided</returns>
        public string GetString(string name)
        {
            string value;
            if (!Values.TryGetValue(name, out value))
            {
                throw new MissingRequiredArgumentException(name);
            }

            return value;
        }

        /// <summary>
        ///  Get a string parameter which is not required.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter or defaultValue if not provided</returns>
        public string GetString(string name, string defaultValue)
        {
            string value;
            if (!Values.TryGetValue(name, out value))
            {
                return defaultValue;
            }

            return value;
        }


        /// <summary>
        ///  Get an int parameter which is required.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter; MissingRequiredArgumentException thrown if not provided</returns>
        public int GetInt(string name)
        {
            string valueString = GetString(name);

            int value;
            if (!int.TryParse(valueString, out value))
            {
                throw new ArgumentIsWrongTypeException(name, valueString, "int");
            }

            return value;
        }

        /// <summary>
        ///  Get an int parameter which is not required.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter or defaultValue if not provided</returns>
        public int GetInt(string name, int defaultValue)
        {
            if (!Values.ContainsKey(name)) return defaultValue;

            string valueString = GetString(name);

            int value;
            if (!int.TryParse(valueString, out value))
            {
                throw new ArgumentIsWrongTypeException(name, valueString, "int");
            }

            return value;
        }

        /// <summary>
        ///  Get a bool parameter which is required.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter; MissingRequiredArgumentException thrown if not provided</returns>
        public bool GetBool(string name)
        {
            string valueString = GetString(name);

            bool value;
            if (!bool.TryParse(valueString, out value))
            {
                throw new ArgumentIsWrongTypeException(name, valueString, "bool");
            }

            return value;
        }

        /// <summary>
        ///  Get a bool parameter which is not required.
        /// </summary>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter or defaultValue if not provided</returns>
        public bool GetBool(string name, bool defaultValue)
        {
            if (!Values.ContainsKey(name)) return defaultValue;

            string valueString = GetString(name);

            bool value;
            if (!bool.TryParse(valueString, out value))
            {
                throw new ArgumentIsWrongTypeException(name, valueString, "bool");
            }

            return value;
        }

        /// <summary>
        ///  Convert the CommandLine back to a parsable form [easy verification of understood parameters].
        /// </summary>
        /// <returns>CommandLine as string form which can be reparsed to the same parameters</returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            foreach (string name in this.Values.Keys)
            {
                if (result.Length > 0) result.Append(" ");

                result.Append("/");
                result.Append(name);
                result.Append(":");

                string valueEscaped = this.Values[name].Replace("\"", "\"\"");
                if (valueEscaped.Contains(" "))
                {
                    valueEscaped = "\"" + valueEscaped + "\"";
                }

                result.Append(valueEscaped);
            }

            return result.ToString();
        }
    }

    [Serializable]
    public class CommandLineUsageException : Exception
    {
        public CommandLineUsageException() { }
        public CommandLineUsageException(string message) : base(message) { }
        public CommandLineUsageException(string message, Exception inner) : base(message, inner) { }
        protected CommandLineUsageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class CommandLineMalformedException : Exception
    {
        public const string CommandLineMalformedMessage = "Command line was unrecognized. Syntax: /name:value /name:\"value with spaces\" /name:\"Literal \"\"Quote\"\"\"\r\n Command Line: {0}";
        public CommandLineMalformedException(string commandLine) : base(String.Format(CommandLineMalformedMessage, commandLine)) { }
        public CommandLineMalformedException(string commandLine, Exception inner) : base(String.Format(CommandLineMalformedMessage, commandLine), inner) { }
        protected CommandLineMalformedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class MissingRequiredArgumentException : CommandLineUsageException
    {
        public const string MissingArgumentMessage = "Required argument '{0}' was not provided.";

        public MissingRequiredArgumentException(string name) : base(String.Format(MissingArgumentMessage, name)) { }
        public MissingRequiredArgumentException(string name, Exception inner) : base(String.Format(MissingArgumentMessage, name), inner) { }
        protected MissingRequiredArgumentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ArgumentIsWrongTypeException : CommandLineUsageException
    {
        public const string ArgumentIsWrongTypeMessage = "Argument '{0}' value '{1}' was not the required type, {2}.";

        public ArgumentIsWrongTypeException() { }
        public ArgumentIsWrongTypeException(string name, string value, string type) : base(String.Format(ArgumentIsWrongTypeMessage, name, value, type)) { }
        public ArgumentIsWrongTypeException(string name, string value, string type, Exception inner) : base(String.Format(ArgumentIsWrongTypeMessage, name, value, type), inner) { }
        protected ArgumentIsWrongTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
