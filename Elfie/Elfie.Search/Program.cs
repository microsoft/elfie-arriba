// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using CommandLine;

namespace Microsoft.CodeAnalysis.Elfie.Search
{
    public class SearchOptions
    {
        public const string Usage = @"
Provides interactive symbol search and navigation with Visual Studio.
Run without arguments to index the current directory.
Press Enter to navigate to the first search result in Visual Studio.
Press F12 to Go To Definition using this data on the relevant Visual Studio.
Press Shift+Enter to open all matches in the relevant Visual Studio.

Elfie.Search
  Index the current directory and allow interactive search

Elfie.Search -d NuGet.All.95.ardb
  Open the 'NuGet.All.95.ardb' database for interactive search

Elfie.Search -q String8Set. -i false -c 50
  Search the current directory case sensitive for 'String8Set.' and output the first 50 results";

        [Option('d', HelpText = "Path to database to load, defaults to .elfie folder in current directory.")]
        public string DatabasePath { get; set; }

        [Option('q', HelpText = "Query to run immediately and exit. If omitted, Elfie runs interactively.")]
        public string Query { get; set; }

        [Option('i', Default = true, HelpText = "Whether to run with case sensitive search.")]
        public bool IgnoreCase { get; set; }

        [Option('c', Default = 30, HelpText = "Number of results to display.")]
        public int ResultCount { get; set; }

        public bool CommandLineQuery
        {
            get { return !String.IsNullOrEmpty(this.Query); }
        }

        public SearchOptions()
        {
            string currentDirectory = Environment.CurrentDirectory;
            DatabasePath = Path.Combine(currentDirectory, ".elfie", Path.GetFileName(currentDirectory) + ".idx");
        }
    }

    public enum SearchExitCode
    {
        Success = 0,
        ArgumentsInvalid = -1,
        UnhandledException = -2
    }

    public class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<SearchOptions>(args);

            try
            {
                return (int)result.MapResult(
                    (arguments) =>
                    {
                        using (ConsoleInterface ci = new ConsoleInterface(arguments))
                        {
                            return ci.Run();
                        }
                    },
                    (errors) =>
                    {
                        Console.WriteLine(SearchOptions.Usage);
                        return (int)SearchExitCode.ArgumentsInvalid;
                    }
                );
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine("Error: " + ex.Message);
                return (int)SearchExitCode.UnhandledException;
            }
        }
    }
}
