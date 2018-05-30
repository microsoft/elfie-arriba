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
        Comment,
        NextTokenHint
    }

    public class Position
    {
        public int Index { get; set; }
        public int LineNumber { get; set; }
        public int LastNewlineIndex { get; set; }
        public int CharInLine => (Index - LastNewlineIndex);

        public Position(int index, int lineNumber, int lastNewlineIndex)
        {
            this.Index = index;
            this.LineNumber = lineNumber;
            this.LastNewlineIndex = lastNewlineIndex;
        }

        public Position(Position other) : this(other.Index, other.LineNumber, other.LastNewlineIndex)
        { }

        public override string ToString()
        {
            return $"({LineNumber}, {CharInLine})";
        }
    }

    public class Token
    {
        public TokenType Type { get; set; }             // Type of current token
        public string Value { get; set; }               // The interpreted value (column name without braces, value unquoted and with escaped quotes interpreted, ...)
        public string RawValue { get; set; }            // The exact value from the query being parsed
        public string WhitespacePrefix { get; set; }    // The whitespace before this token, if any
        public bool IsWrapped { get; set; }             // Whether this token was wrapped (in quotes or braces), and so had an explicit end marker

        public Position Position { get; set; }

        public Token(Position position)
        {
            this.Type = TokenType.End;
            this.Value = "";
            this.RawValue = "";
            this.WhitespacePrefix = "";
            this.IsWrapped = false;

            // Make a snapshot of the other position
            this.Position = new Position(position);
        }
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
        private Position Position { get; set; }

        public Token Current { get; set; }

        static XqlScanner()
        {
            s_charactersRequiringEscaping = new HashSet<char>(new char[] { ' ', '\t', '"', '[', ']', '(', ')', ',', '~' });
        }

        public XqlScanner(string xqlQuery)
        {
            this.Text = xqlQuery;
            this.Position = new Position(0, 1, -1);
            Current = new Token(this.Position) { Type = TokenType.Newline };
            Next();
        }

        public void RewindTo(Position position)
        {
            // Go back to the start of the token and re-read it
            this.Position = new Position(position);
            InnerNext();
        }

        public bool Next()
        {
            TokenType lastType = this.Current.Type;
            bool result = false;

            // If the caller advances the end again, report whitespace only once [easier logic for 'is last token' error message rules]
            if (this.Current.Type == TokenType.End) this.Current.WhitespacePrefix = "";

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
            string whitespace = ReadWhitespace();
            this.Current = new Token(this.Position) { WhitespacePrefix = whitespace };

            if (this.Position.Index >= Text.Length) return false;

            char next = Text[this.Position.Index];
            if (next == '\r' || next == '\n')
            {
                this.Position.Index++;
                if (next == '\r' || Peek() == '\n') this.Position.Index++;
                this.Position.LineNumber++;
                this.Position.LastNewlineIndex = this.Position.Index;
                this.Current.Type = TokenType.Newline;
            }
            else if (next == '~')
            {
                this.Current.Type = TokenType.NextTokenHint;
            }
            else if (next == '#')
            {
                ParseUntilNewline();
                this.Current.Type = TokenType.Comment;
            }
            else if (next == '(')
            {
                this.Position.Index++;
                this.Current.Type = TokenType.OpenParen;
            }
            else if (next == ')')
            {
                this.Position.Index++;
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

            // Get the unescaped, un-interpreted value of the current token
            this.Current.RawValue = this.Text.Substring(this.Current.Position.Index, this.Position.Index - this.Current.Position.Index);

            return true;
        }

        private char Peek()
        {
            if (this.Position.Index < Text.Length) return Text[this.Position.Index];
            return '\0';
        }

        private string ReadWhitespace()
        {
            int whitespaceLength;
            for (whitespaceLength = 0; this.Position.Index + whitespaceLength < Text.Length; ++whitespaceLength)
            {
                char current = Text[this.Position.Index + whitespaceLength];
                if (current != ' ' && current != '\t' && current != ',') break;
            }

            if (whitespaceLength == 0) return string.Empty;

            string whitespace = Text.Substring(this.Position.Index, whitespaceLength);
            this.Position.Index += whitespaceLength;
            return whitespace;
        }

        private void ParseWrappedValue(char escapeChar)
        {
            StringBuilder value = new StringBuilder();

            // Consume the opening character
            this.Position.Index++;

            // If we're here, mark the token as wrapped
            this.Current.IsWrapped = true;

            // Find the end for unterminated values (the next newline or the end of the query)
            int end = Text.IndexOf('\n', this.Position.Index);
            if (end == -1) end = Text.Length;
            else if (Text[end - 1] == '\r') end--;

            while (this.Position.Index < end)
            {
                int nextEscape = Text.IndexOf(escapeChar, this.Position.Index);
                if (nextEscape == -1) break;

                if (Text.Length > (nextEscape + 1) && Text[nextEscape + 1] == escapeChar)
                {
                    // Escaped Value of Escape Character - append the value so far including one copy and keep searching for the end
                    value.Append(Text, this.Position.Index, nextEscape - this.Position.Index + 1);
                    this.Position.Index = nextEscape + 2;
                }
                else
                {
                    // Value Terminator. Append the value without the terminator and return it
                    value.Append(Text, this.Position.Index, nextEscape - this.Position.Index);
                    this.Position.Index = nextEscape + 1;
                    this.Current.Value = value.ToString();
                    return;
                }
            }

            // If no terminator, treat the value as going to the end of the line. This is so partially typed queries run.
            value.Append(Text, this.Position.Index, end - this.Position.Index);
            this.Position.Index = end;
            this.Current.Value = value.ToString();
        }

        private void ParseUnwrappedValue()
        {
            int startIndex = this.Position.Index;
            while (this.Position.Index < Text.Length)
            {
                char current = Text[this.Position.Index];
                if (Char.IsWhiteSpace(current) || current == '(' || current == ')' || current == ',') break;
                this.Position.Index++;
            }

            this.Current.IsWrapped = false;
            this.Current.Value = Text.Substring(startIndex, this.Position.Index - startIndex);
        }

        private void ParseUntilNewline()
        {
            int startIndex = this.Position.Index;
            while (this.Position.Index < Text.Length)
            {
                char current = Text[this.Position.Index];
                if (current == '\r' || current == '\n') break;
                this.Position.Index++;
            }

            this.Current.Value = Text.Substring(startIndex, this.Position.Index - startIndex);
        }

        public static string Escape(object value, TokenType type, bool wasUnwrapped = false)
        {
            return Escape((value == null ? null : value.ToString()), type, wasUnwrapped);
        }

        public static string Escape(string value, TokenType type, bool wasUnwrapped = false)
        {
            if (type == TokenType.ColumnName)
            {
                if (String.IsNullOrEmpty(value)) return "[]";

                // Always escape column names
                return "[" + value.Replace("]", "]]") + "]";
            }
            else if (type == TokenType.Value)
            {
                if (String.IsNullOrEmpty(value)) return "\"\"";
                if (wasUnwrapped && !RequiresEscaping(value)) return value;
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

        public static string QueryToSingleLineStyle(string query)
        {
            StringBuilder result = new StringBuilder();

            foreach (string line in query.Split('\n'))
            {
                string cleanLine = line.TrimEnd('\r').Trim();
                if (!String.IsNullOrEmpty(cleanLine))
                {
                    if (result.Length > 0) result.Append(" | ");
                    result.Append(cleanLine);
                }
            }

            return result.ToString();
        }
    }
}
