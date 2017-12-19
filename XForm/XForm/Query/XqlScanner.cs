// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace XForm.Query
{
    public enum TokenType : byte
    {
        End,
        ColumnName,
        FunctionName,
        Value,
        Newline,
        OpenParen,
        CloseParen,
        Comment
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public string WhitespacePrefix { get; set; }

        public int LineNumber { get; set; }
        public int CharInLine { get; set; }
    }

    /// <summary>
    ///  Query: QueryLine+
    ///  QueryLine: Comment | Verb Argument+ Newline
    ///  Comment: '#' ... Newline
    ///  Argument: 
    ///  Scalar: ColumnName | Constant | Function
    ///  ColumnName: '[' Literal ']'
    ///  Constant: '"' Literal '"'
    ///  Function: Literal '(' Argument* ')'
    ///  TableName: '"' Literal '"'
    /// </summary>
    public class XqlScanner
    {
        private static HashSet<char> s_charactersRequiringEscaping;

        private string Text { get; set; }
        private int CurrentIndex { get; set; }
        private int LastNewlineIndex { get; set; }
        private int CurrentLineNumber { get; set; }

        public Token Current { get; set; }

        static XqlScanner()
        {
            s_charactersRequiringEscaping = new HashSet<char>(new char[] { ' ', '\t', '"', '[', ']', '(', ')', ',' });
        }

        public XqlScanner(string xqlQuery)
        {
            this.Text = xqlQuery;
            Current = new Token() { Type = TokenType.Newline };
            LastNewlineIndex = -1;
            CurrentLineNumber = 1;
            Next();
        }

        public bool Next()
        {
            TokenType lastType = this.Current.Type;
            bool result = false;

            while (this.Current.Type != TokenType.End)
            {
                // Get the next token
                result = InnerNext();

                // Skip comments (always)
                if (this.Current.Type == TokenType.Comment) continue;

                // Merge together newlines
                if (lastType == TokenType.Newline && this.Current.Type == TokenType.Newline) continue;

                break;
            }

            return result;
        }

        public bool InnerNext()
        {
            this.Current = new Token();
            this.Current.Type = TokenType.End;
            this.Current.Value = "";
            this.Current.LineNumber = CurrentLineNumber;

            ReadWhitespace();

            if (CurrentIndex >= Text.Length) return false;
            this.Current.CharInLine = this.CurrentIndex - this.LastNewlineIndex;

            char next = Text[CurrentIndex];
            if (next == '\r' || next == '\n')
            {
                CurrentIndex++;
                if (next == '\r' || Peek() == '\n') CurrentIndex++;
                this.CurrentLineNumber++;
                this.LastNewlineIndex = CurrentIndex;
                this.Current.Type = TokenType.Newline;
            }
            else if (next == '#')
            {
                ParseUntilNewline();
                this.Current.Type = TokenType.Comment;
            }
            else if (next == '(')
            {
                CurrentIndex++;
                this.Current.Type = TokenType.OpenParen;
            }
            else if (next == ')')
            {
                CurrentIndex++;
                this.Current.Type = TokenType.CloseParen;
            }
            else if (next == '[')
            {
                ParseWrappedValue(']');
                this.Current.Type = TokenType.ColumnName;
            }
            else if (next == '"')
            {
                ParseWrappedValue('"');
                this.Current.Type = TokenType.Value;
            }
            else
            {
                ParseUnwrappedValue();
                this.Current.Type = (Peek() == '(' ? TokenType.FunctionName : TokenType.Value);
            }

            return true;
        }

        private char Peek()
        {
            if (CurrentIndex < Text.Length) return Text[CurrentIndex];
            return '\0';
        }

        private void ReadWhitespace()
        {
            int whitespaceLength;
            for (whitespaceLength = 0; CurrentIndex + whitespaceLength < Text.Length; ++whitespaceLength)
            {
                char current = Text[CurrentIndex + whitespaceLength];
                if (current != ' ' && current != '\t' && current != ',') break;
            }

            if (whitespaceLength > 0)
            {
                this.Current.WhitespacePrefix = Text.Substring(CurrentIndex, whitespaceLength);
                this.CurrentIndex += whitespaceLength;
            }
            else
            {
                this.Current.WhitespacePrefix = String.Empty;
            }
        }

        private void ParseWrappedValue(char escapeChar)
        {
            StringBuilder value = new StringBuilder();

            CurrentIndex++;
            while (CurrentIndex < Text.Length)
            {
                int nextEscape = Text.IndexOf(escapeChar, CurrentIndex);
                if (nextEscape == -1) break;

                if (Text.Length > (nextEscape + 1) && Text[nextEscape + 1] == escapeChar)
                {
                    // Escaped Value of Escape Character - append the value so far including one copy and keep searching for the end
                    value.Append(Text, CurrentIndex, nextEscape - CurrentIndex + 1);
                    CurrentIndex = nextEscape + 2;
                }
                else
                {
                    // Value Terminator. Append the value without the terminator and return it
                    value.Append(Text, CurrentIndex, nextEscape - CurrentIndex);
                    CurrentIndex = nextEscape + 1;
                    this.Current.Value = value.ToString();
                    return;
                }
            }

            // If no terminator, treat the value as going to the end of the line. This is so partially typed queries run.
            value.Append(Text, CurrentIndex, Text.Length - CurrentIndex);
            CurrentIndex = Text.Length;
            this.Current.Value = value.ToString();
        }

        private void ParseUnwrappedValue()
        {
            int startIndex = CurrentIndex;
            while (CurrentIndex < Text.Length)
            {
                char current = Text[CurrentIndex];
                if (Char.IsWhiteSpace(current) || current == '(' || current == ')' || current == ',') break;
                CurrentIndex++;
            }

            this.Current.Value = Text.Substring(startIndex, CurrentIndex - startIndex);
        }

        private void ParseUntilNewline()
        {
            int startIndex = CurrentIndex;
            while (CurrentIndex < Text.Length)
            {
                char current = Text[CurrentIndex];
                if (current == '\r' || current == '\n') break;
                CurrentIndex++;
            }

            this.Current.Value = Text.Substring(startIndex, CurrentIndex - startIndex);
        }

        public static string Escape(string value, TokenType type)
        {
            if (type == TokenType.ColumnName)
            {
                if (String.IsNullOrEmpty(value)) return "[]";
                if (!RequiresEscaping(value)) return value;
                return "[" + value.Replace("]", "]]") + "]";
            }
            else if (type == TokenType.Value)
            {
                if (String.IsNullOrEmpty(value)) return "\"\"";
                if (!RequiresEscaping(value)) return value;
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                throw new NotSupportedException($"XqlScanner can't escape token type {type}.");
            }
        }

        public static IEnumerable<string> Escape(IEnumerable<string> values, TokenType type)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    yield return Escape(value, type);
                }
            }
        }

        private static bool RequiresEscaping(string value)
        {
            if (String.IsNullOrEmpty(value)) return false;

            for (int i = 0; i < value.Length; ++i)
            {
                if (s_charactersRequiringEscaping.Contains(value[i])) return true;
            }

            return false;
        }
    }
}
