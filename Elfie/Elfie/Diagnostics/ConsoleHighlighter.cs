// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  ConsoleHighlighter writes a string value to the console, highlighting
    ///  all instances of a given substring.
    /// </summary>
    public static class ConsoleHighlighter
    {
        public static void WriteWithHighlight(string content, string query)
        {
            Console.ResetColor();

            if (String.IsNullOrEmpty(query))
            {
                Console.Write(content);
                return;
            }

            int lastIndexWritten = 0;
            while (lastIndexWritten < content.Length)
            {
                int nextHighlightIndex = content.IndexOf(query, lastIndexWritten, StringComparison.OrdinalIgnoreCase);

                // If no more matches, stop
                if (nextHighlightIndex == -1) break;

                // Write unhighlighted prefix
                int length = nextHighlightIndex - lastIndexWritten;
                if (length > 0)
                {
                    Console.Write(content.Substring(lastIndexWritten, length));
                    lastIndexWritten += length;
                }

                // Write highlighted value
                SetHighlightColors();
                Console.Write(content.Substring(lastIndexWritten, query.Length));
                lastIndexWritten += query.Length;
                Console.ResetColor();
            }

            // Write the suffix after the last match
            Console.Write(content.Substring(lastIndexWritten));
        }

        private static void SetHighlightColors()
        {
            switch (Console.ForegroundColor)
            {
                case ConsoleColor.White:
                case ConsoleColor.Gray:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case ConsoleColor.Yellow:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case ConsoleColor.Black:
                case ConsoleColor.DarkBlue:
                case ConsoleColor.DarkCyan:
                case ConsoleColor.DarkGreen:
                case ConsoleColor.DarkMagenta:
                case ConsoleColor.DarkRed:
                case ConsoleColor.DarkYellow:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case ConsoleColor.DarkGray:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
            }
        }
    }
}
