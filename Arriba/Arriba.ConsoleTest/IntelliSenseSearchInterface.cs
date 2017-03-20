using Arriba.Model.Query;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Arriba.ConsoleTest
{
    /// <summary>
    ///  Wrapper to provide a simple interactive Console search interface,
    ///  given a query method and an output method.
    /// </summary>
    /// <typeparam name="T">Type of item being searched</typeparam>
    public class IntelliSenseConsoleSearchInterface<T>
    {
        private int SuggestLimitToShow { get; set; }
        private int LimitToShow { get; set; }

        private string Query { get; set; }
        private Position Start { get; set; }
        private Position QueryEnd { get; set; }
        private Position End { get; set; }

        private Func<string, SearchResult<T>> Search { get; set; }
        private Action<T, StringBuilder> Write { get; set; }

        private Arriba.Model.Query.IntelliSenseResult IntelliSenseResult { get; set; }
        private Func<string, IntelliSenseResult> Suggest { get; set; }
        private Func<string, IntelliSenseResult, IntelliSenseItem, char, string> Complete { get; set; }

        public IntelliSenseConsoleSearchInterface(Func<string, SearchResult<T>> searchMethod, Action<T, StringBuilder> writeMethod, int limitToShow = 20, int suggestLimitToShow = 10)
        {
            this.Query = String.Empty;
            this.Start = new Position();
            this.QueryEnd = new Position();
            this.End = new Position();

            this.Search = searchMethod;
            this.Write = writeMethod;
            this.LimitToShow = limitToShow;
            this.SuggestLimitToShow = suggestLimitToShow;
        }

        public void ConfigureIntelliSense(Func<string, IntelliSenseResult> suggestMethod, Func<string, IntelliSenseResult, IntelliSenseItem, char, string> completeMethod)
        {
            this.Suggest = suggestMethod;
            this.Complete = completeMethod;
        }

        public void Run()
        {
            string showingLimit = String.Format(" Showing {0:n0}.", this.LimitToShow);
            string showingSuggestLimit = String.Format(" Showing {0:n0}.", this.SuggestLimitToShow);
            Stopwatch w = Stopwatch.StartNew();

            Console.Write("> ");
            this.Start.Save();
            this.End.Save();

            while (ReadKey())
            {
                // Clear the previous results
                this.Start.ClearUpTo(this.End);

                // Write the query, tracking the position right afterward
                Console.Write(this.Query);
                this.QueryEnd.Save();

                StringBuilder output = new StringBuilder();

                if (this.Suggest != null)
                {
                    // Get IntelliSense
                    w.Restart();
                    this.IntelliSenseResult = this.Suggest(this.Query);
                    w.Stop();

                    // Write summary line
                    if (this.IntelliSenseResult != null)
                    {
                        output.AppendFormat("\r\n{0:n0} suggestions for \"{1}\" in {2}.", this.IntelliSenseResult.Suggestions.Count, this.Query, w.Elapsed.ToFriendlyString());
                        if (this.IntelliSenseResult.Suggestions.Count > this.SuggestLimitToShow) output.Append(showingSuggestLimit);
                        output.AppendLine();

                        // Write IntelliSense items (first)
                        if (this.IntelliSenseResult != null && this.IntelliSenseResult.Suggestions.Count > 0)
                        {
                            for (int j = 0; j < Math.Min(10, this.IntelliSenseResult.Suggestions.Count); ++j)
                            {
                                IntelliSenseItem item = this.IntelliSenseResult.Suggestions[j];
                                output.AppendFormat("{0}\t{1} [{2}]\r\n", item.Value, item.Hint, item.Category);
                            }
                        }
                    }
                }

                if (!String.IsNullOrEmpty(this.Query))
                {
                    // Find the results
                    w.Restart();
                    SearchResult<T> result = this.Search(this.Query);
                    w.Stop();

                    output.AppendLine();

                    output.AppendFormat("\r\nFound {0:n0} matches for \"{1}\" in {2}.", result.Count, this.Query, w.Elapsed.ToFriendlyString());
                    if (result.Count > this.LimitToShow) output.Append(showingLimit);
                    output.AppendLine();

                    // Write each result
                    int i = 0;
                    if (result.Matches != null)
                    {
                        while (result.Matches.MoveNext())
                        {
                            this.Write(result.Matches.Current, output);
                            if (++i >= this.LimitToShow) break;
                        }
                    }
                }

                // Highlight and output the results
                ConsoleHighlighter.WriteWithHighlight(output.ToString(), this.Query);

                // Track the end of the output
                this.End.Save();

                // Put the cursor back at the end of the query
                this.QueryEnd.Restore();
            }

            this.End.Restore();
        }

        private bool ReadKey()
        {
            ConsoleKeyInfo info = Console.ReadKey(true);
            switch (info.Key)
            {
                case ConsoleKey.Escape:
                    return false;
                case ConsoleKey.Backspace:
                    if (this.Query.Length > 0) this.Query = this.Query.Substring(0, this.Query.Length - 1);
                    return true;
                case ConsoleKey.Delete:
                    this.Query = String.Empty;
                    return true;
                case ConsoleKey.Enter:
                    return false;
                default:
                    char c = info.KeyChar;
                    if (c != '\0')
                    {
                        if (this.IntelliSenseResult != null)
                        {
                            if (this.IntelliSenseResult.CompletionCharacters.Contains(c))
                            {
                                this.Query = this.Complete(this.Query, this.IntelliSenseResult, this.IntelliSenseResult.Suggestions[0], c);
                                return true;
                            }
                        }

                        this.Query += c;
                    }
                    return true;
            }
        }
    }
}
