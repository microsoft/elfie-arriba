// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  Enum of different token types which scanner can distinguish.
    /// </summary>
    internal enum TokenType
    {
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        DoubleQuote,
        UnaryOperatorNot,
        BooleanOperatorAnd,
        BooleanOperatorOr,
        CompareOperatorMatches,
        CompareOperatorMatchesExact,
        CompareOperatorLessThan,
        CompareOperatorLessThanOrEqual,
        CompareOperatorGreaterThan,
        CompareOperatorGreaterThanOrEqual,
        CompareOperatorEquals,
        CompareOperatorNotEquals,
        CompareOperatorStartsWith,
        Identifier,
        Other,
        End
    }

    /// <summary>
    ///  Represents a Token parsed from source content of one of the
    ///  TokenType types.
    /// </summary>
    internal class Token
    {
        /// <summary>
        ///  TokenType of this token
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        ///  Whitespace found after the previous token and before the content
        ///  of this token.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        ///  Literal content of this token, from the source.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///  Creates a new Token instance.
        /// </summary>
        public Token()
        {
            this.Prefix = String.Empty;
            this.Content = String.Empty;
        }

        /// <summary>
        ///  Returns the raw content for this token, including original whitespace.
        /// </summary>
        /// <returns>Content for this token, including whitespace</returns>
        public override string ToString()
        {
            return Prefix + Content;
        }
    }

    /// <summary>
    ///  A Scanner (tokenizer) for Query Where Clauses; splits source into a stream of
    ///  tokens for parsing. The scanner splits and keeps the whitespace between
    ///  tokens in the Prefix of each Token.
    /// </summary>
    internal class QueryScanner
    {
        /// <summary>
        ///  Constant values for literal tokens.
        ///  IMPORTANT: Must be in descending length order.
        /// </summary>
        private static Tuple<string, TokenType>[] s_literals = new Tuple<string, TokenType>[]
        {
            new Tuple<string, TokenType>("STARTSWITH", TokenType.CompareOperatorStartsWith ),
            new Tuple<string, TokenType>("MATCHEXACT", TokenType.CompareOperatorMatchesExact ),
            new Tuple<string, TokenType>("FREETEXT", TokenType.CompareOperatorMatches ),
            new Tuple<string, TokenType>("CONTAINS", TokenType.CompareOperatorMatches ),
            new Tuple<string, TokenType>("UNDER", TokenType.CompareOperatorStartsWith ),
            new Tuple<string, TokenType>("MATCH", TokenType.CompareOperatorMatches ),
            new Tuple<string, TokenType>("LIKE", TokenType.CompareOperatorMatches ),
            new Tuple<string, TokenType>("NOT", TokenType.UnaryOperatorNot ),
            new Tuple<string, TokenType>("AND", TokenType.BooleanOperatorAnd ),
            new Tuple<string, TokenType>("OR", TokenType.BooleanOperatorOr ),
            new Tuple<string, TokenType>("&&", TokenType.BooleanOperatorAnd ),
            new Tuple<string, TokenType>("||", TokenType.BooleanOperatorOr ),
            new Tuple<string, TokenType>("::", TokenType.CompareOperatorMatchesExact ),
            new Tuple<string, TokenType>("|>", TokenType.CompareOperatorStartsWith ),
            new Tuple<string, TokenType>("<=", TokenType.CompareOperatorLessThanOrEqual ),
            new Tuple<string, TokenType>(">=", TokenType.CompareOperatorGreaterThanOrEqual ),
            new Tuple<string, TokenType>("==", TokenType.CompareOperatorEquals ),
            new Tuple<string, TokenType>("!=", TokenType.CompareOperatorNotEquals ),
            new Tuple<string, TokenType>("<>", TokenType.CompareOperatorNotEquals ),
            new Tuple<string, TokenType>("(", TokenType.LeftParen ),
            new Tuple<string, TokenType>(")", TokenType.RightParen ),
            new Tuple<string, TokenType>("[", TokenType.LeftBrace ),
            new Tuple<string, TokenType>("]", TokenType.RightBrace ),
            new Tuple<string, TokenType>("\"", TokenType.DoubleQuote ),
            new Tuple<string, TokenType>("-", TokenType.UnaryOperatorNot ),
            new Tuple<string, TokenType>("!", TokenType.UnaryOperatorNot ),
            new Tuple<string, TokenType>("&", TokenType.BooleanOperatorAnd ),
            new Tuple<string, TokenType>("|", TokenType.BooleanOperatorOr ),
            new Tuple<string, TokenType>(":", TokenType.CompareOperatorMatches ),
            new Tuple<string, TokenType>("<", TokenType.CompareOperatorLessThan ),
            new Tuple<string, TokenType>(">", TokenType.CompareOperatorGreaterThan ),
            new Tuple<string, TokenType>("=", TokenType.CompareOperatorEquals )
        };

        /// <summary>
        ///  Characters which indicate the end of an identifier - basically the set of characters which
        ///  literals may start with.
        /// </summary>
        private static HashSet<Char> s_nonIdentifiers;

        /// <summary>
        ///  The string of the full original Html to be split.
        /// </summary>
        private string Text { get; set; }

        /// <summary>
        ///  The index in the Text where the next token begins (just after
        ///  the last token found).
        /// </summary>
        private int CurrentIndex { get; set; }

        /// <summary>
        ///  The current line number where the token begins
        /// </summary>
        private int LineNumber { get; set; }

        /// <summary>
        ///  The character index in the current line where the token begins
        /// </summary>
        private int CharInLine { get; set; }

        /// <summary>
        ///  The current Token read. Next() is called to read the next
        ///  token and update Current.
        /// </summary>
        public Token Current { get; private set; }

        static QueryScanner()
        {
            // Initialize NonIdentifiers, if needed (only one instance is required)
            if (s_nonIdentifiers == null)
            {
                s_nonIdentifiers = new HashSet<char>();
                foreach (Tuple<string, TokenType> literal in s_literals)
                {
                    char firstLetter = literal.Item1[0];
                    if (!Char.IsLetter(firstLetter)) s_nonIdentifiers.Add(firstLetter);
                }
            }
        }

        /// <summary>
        ///  Create a new Scanner to read the given Html source document.
        /// </summary>
        /// <param name="reader">Reader from which to read</param>
        public QueryScanner(TextReader reader)
        {
            this.Text = reader.ReadToEnd();
            this.CurrentIndex = 0;

            this.LineNumber = 1;
            this.CharInLine = 0;
        }

        /// <summary>
        ///  Return true if this is the last token (the next one will be the end)
        ///  specifically without following whitespace.
        /// </summary>
        /// <returns>true if there are no more tokens or trailing whitespace, false otherwise</returns>
        public bool IsLastToken()
        {
            return CurrentIndex >= Text.Length;
        }

        /// <summary>
        ///  Advance the scanner to the next Token in the source. Returns whether
        ///  the end of the input has been reached.
        /// </summary>
        /// <returns>True if another token remains, False if there are no more tokens</returns>
        public bool Next()
        {
            UpdatePosition();

            this.Current = new Token();

            // First, read any whitespace into the prefix
            ReadWhitespace();

            // If there's no remaining content, stop
            if (CurrentIndex >= Text.Length)
            {
                Current.Content = String.Empty;
                Current.Type = TokenType.End;
                return false;
            }

            bool tokenFound = false;

            // Is there a literal here?
            if (!tokenFound) tokenFound = ReadLiteral();

            // Is there an identifier here?
            if (!tokenFound) tokenFound = ReadIdentifier();

            // If not, return as "other"
            if (!tokenFound)
            {
                Current.Content = Text.Substring(CurrentIndex, 1);
                Current.Type = TokenType.Other;
                CurrentIndex++;
            }

            return true;
        }

        /// <summary>
        ///  Verify the current token is a certain type and throw if not
        /// </summary>
        /// <param name="caller">Name of type being parsed (for error)</param>
        /// <param name="allowedTypes">Type(s) expected here</param>
        public bool Expect(string caller, params TokenType[] allowedTypes)
        {
            foreach (TokenType allowedType in allowedTypes)
            {
                if (Current.Type == allowedType) return true;
            }

            //throw new HtmlParseException(StringExtensions.Format("({0}, {1}): {2} found {3} ('{4}') where {5} was expected", LineNumber, CharInLine, caller, Current.Type, Current.Content, String.Join(" or ", allowedTypes)));
            return false;
        }

        /// <summary>
        ///  Communicate a warning during scanning or parsing. Used to communicate
        ///  non-fatal problems in the Html seen.
        /// </summary>
        /// <param name="caller">The component reporting the warning</param>
        /// <param name="warning">Warning message indicating the problem</param>
        public void Warn(string caller, string warning)
        {
            Console.WriteLine("Parse Warning (Index {0}, Parsing {1}): {2}", CurrentIndex, caller, warning);
        }

        /// <summary>
        ///  Read and store any whitespace at the current index. Whitespace is stored in Current.Prefix.
        /// </summary>
        /// <returns>True if whitespace read, false if there wasn't any</returns>
        private bool ReadWhitespace()
        {
            int whitespaceLength;
            for (whitespaceLength = 0; CurrentIndex + whitespaceLength < Text.Length; ++whitespaceLength)
            {
                if (!Char.IsWhiteSpace(Text[CurrentIndex + whitespaceLength])) break;
            }

            if (whitespaceLength > 0)
            {
                this.Current.Prefix = Text.Substring(CurrentIndex, whitespaceLength);
                this.CurrentIndex += whitespaceLength;
                return true;
            }
            else
            {
                this.Current.Prefix = String.Empty;
            }

            return false;
        }

        /// <summary>
        ///  Read a literal at the current index, if any. Type and Content are stored in Current.
        /// </summary>
        /// <returns>True if a literal was found, false otherwise</returns>
        private bool ReadLiteral()
        {
            int remainingLength = Text.Length - CurrentIndex;

            for (int i = 0; i < s_literals.Length; ++i)
            {
                string literalToMatch = s_literals[i].Item1;

                // If there's not enough file left for this literal to be there, no need to match
                if (remainingLength < literalToMatch.Length) continue;

                // Try to match this literal
                bool literalIsAlpha = true;
                int j;
                for (j = 0; j < literalToMatch.Length; ++j)
                {
                    if (Char.ToUpperInvariant(Text[CurrentIndex + j]) != literalToMatch[j]) break;
                    if (!Char.IsLetter(literalToMatch[j])) literalIsAlpha = false;
                }

                // If it's a complete match, return it and move forward
                if (j == literalToMatch.Length)
                {
                    // If this literal is all letters, require a non-letter after it
                    if (literalIsAlpha && remainingLength > literalToMatch.Length)
                    {
                        char nextCharacter = Text[CurrentIndex + literalToMatch.Length];
                        if (Char.IsLetter(nextCharacter)) continue;
                    }

                    // Otherwise, consider this a match and return
                    this.Current.Type = s_literals[i].Item2;
                    this.Current.Content = Text.Substring(CurrentIndex, literalToMatch.Length);
                    this.CurrentIndex += literalToMatch.Length;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///  Read an identifier at the current index, if found. The Content is stored in Current.
        ///  Identifiers are any content until a non-identifier character is seen, and consume 
        ///  literal text as well as element and attribute names and non-symbol parts of string values.
        /// </summary>
        /// <returns>True if an identifier was read, false otherwise.</returns>
        private bool ReadIdentifier()
        {
            int identifierLength = 0;

            // Read until the next whitespace or other token start
            for (identifierLength = 0; CurrentIndex + identifierLength < Text.Length; ++identifierLength)
            {
                char current = Text[CurrentIndex + identifierLength];
                if (Char.IsWhiteSpace(current) || s_nonIdentifiers.Contains(current)) break;
            }

            if (identifierLength > 0)
            {
                this.Current.Type = TokenType.Identifier;
                this.Current.Content = Text.Substring(CurrentIndex, identifierLength);
                CurrentIndex += identifierLength;
                return true;
            }

            return false;
        }

        /// <summary>
        ///  Update the current line and character position for where the next token begins
        /// </summary>
        private void UpdatePosition()
        {
            if (this.Current == null) return;

            UpdatePosition(this.Current.Prefix);
            UpdatePosition(this.Current.Content);
        }

        private void UpdatePosition(string value)
        {
            if (String.IsNullOrEmpty(value)) return;

            foreach (char c in value)
            {
                this.CharInLine++;

                if (c == '\n')
                {
                    this.LineNumber++;
                    this.CharInLine = 1;
                }
            }
        }
    }
}
