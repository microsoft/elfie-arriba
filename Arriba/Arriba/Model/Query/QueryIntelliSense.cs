using System;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  Enum of different broad token categories which the last token could be.
    /// </summary>
    [Flags]
    public enum CurrentTokenCategory : byte
    {
        None = 0x0,
        BooleanOperator = 0x1,
        ColumnName = 0x2,
        CompareOperator = 0x4,
        TermPrefixes = 0x8,
        Value = 0x10,
        Term = TermPrefixes | ColumnName | Value
    }

    public struct IntelliSenseGuidance
    {
        public string Value;
        public CurrentTokenCategory Options;

        public IntelliSenseGuidance(string value, CurrentTokenCategory options)
        {
            this.Value = value;
            this.Options = options;
        }

        public override string ToString()
        {
            return String.Format("[{0}] [{1}]", this.Value, this.Options);
        }
    }
}
