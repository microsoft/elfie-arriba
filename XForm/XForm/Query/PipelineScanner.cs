using System;
using System.Collections.Generic;
using System.Text;

namespace XForm.Query
{
    public class PipelineScanner
    {
        private List<string> _queryLines;
        private List<string> _currentLineParts;
        private int _currentLineIndex;
        private int _currentPartIndex;

        public PipelineScanner(string xqlQuery)
        {
            _queryLines = new List<string>();

            foreach (string queryLine in xqlQuery.Split('\n'))
            {
                string cleanedLine = queryLine.TrimEnd('\r').Trim();
                if (!String.IsNullOrEmpty(cleanedLine)) _queryLines.Add(cleanedLine);
            }

            _currentLineIndex = -1;
            NextLine();
        }

        public bool HasCurrentLine => _currentLineIndex < _queryLines.Count;
        public string CurrentLine => (HasCurrentLine ? _queryLines[_currentLineIndex] : null);
        public IList<string> CurrentLineParts => (HasCurrentLine ? _currentLineParts : null);
        public int CurrentLineNumber => (HasCurrentLine ? _currentLineIndex + 1 : -1);

        public bool HasCurrentPart => (HasCurrentLine && _currentPartIndex < _currentLineParts.Count);
        public string CurrentPart => (HasCurrentPart ? _currentLineParts[_currentPartIndex] : null);

        public bool NextLine()
        {
            _currentLineIndex++;
            _currentPartIndex = 0;
            if (_currentLineIndex >= _queryLines.Count) return false;

            _currentLineParts = SplitLine(_queryLines[_currentLineIndex]);

            return true;
        }

        public bool NextPart()
        {
            _currentPartIndex++;
            return _currentPartIndex < _currentLineParts.Count;
        }

        private static List<string> SplitLine(string xqlQueryLine)
        {
            List<string> parts = new List<string>();

            int index = 0;
            while (true)
            {
                string part = SplitLinePart(xqlQueryLine, ref index);
                if (part == null) break;
                parts.Add(part);
            }

            return parts;
        }

        private static string SplitLinePart(string configurationText, ref int index)
        {
            // Ignore whitespace before the value
            while (index < configurationText.Length && (Char.IsWhiteSpace(configurationText[index]) || configurationText[index] == ',')) index++;

            // If this is the end of the text, return null
            if (index == configurationText.Length) return null;

            if (configurationText[index] == '"')
            {
                // Quoted value. Read until an end quote, treating "" as an escaped quote.
                StringBuilder value = new StringBuilder();

                index++;
                while (index < configurationText.Length)
                {
                    int nextQuote = configurationText.IndexOf('"', index);
                    if (nextQuote == -1) break;

                    if (configurationText.Length > (nextQuote + 1) && configurationText[nextQuote + 1] == '"')
                    {
                        // Escaped Quote - append the value so far including one quote and keep searching for the end
                        value.Append(configurationText, index, nextQuote - index + 1);
                        index = nextQuote + 2;
                    }
                    else
                    {
                        // Closing Quote. Append the value without the quote and return it
                        value.Append(configurationText, index, nextQuote - index);
                        index = nextQuote + 1;
                        return value.ToString();
                    }
                }

                // If no closing quote, treat the value as going to the end of the line. This is so partially typed queries run.
                value.Append(configurationText, index, configurationText.Length - index);
                index = configurationText.Length;
                return value.ToString();
            }
            else
            {
                // Unquoted value. Return value until next whitespace or end of string
                int start = index;
                while (index < configurationText.Length && !(Char.IsWhiteSpace(configurationText[index]) || configurationText[index] == ',')) index++;
                return configurationText.Substring(start, index - start);
            }
        }

        private static char[] s_escapeRequiredCharacters = new char[] { '"', '\t', ' ', ',' };
        public static string Escape(string value)
        {
            if (String.IsNullOrEmpty(value)) return "\"\"";
            if (value.IndexOfAny(s_escapeRequiredCharacters) == -1) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static IEnumerable<string> Escape(IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    yield return Escape(value);
                }
            }
        }
    }

}
