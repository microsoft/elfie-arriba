// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Arriba.Extensions;
using Arriba.Indexing;
using Arriba.Model.Query;
using Arriba.Structures;

namespace Arriba.Model.Expressions
{
    public class OrExpression : IExpression
    {
        private IExpression[] _set;

        public OrExpression(params IExpression[] set)
        {
            if (set != null)
            {
                _set = set.Where(s => s != null).ToArray();
            }
        }

        public void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (partition == null) throw new ArgumentNullException("partition");
            if (result == null) throw new ArgumentNullException("result");

            ushort itemCount = partition.Count;

            foreach (IExpression part in _set)
            {
                part.TryEvaluate(partition, result, details);
                if (result.Count() == itemCount) break;
            }
        }

        public IList<IExpression> Children()
        {
            return _set;
        }

        public override string ToString()
        {
            return String.Join(" OR ", (IList<IExpression>)_set);
        }
    }

    public class AndExpression : IExpression
    {
        private IExpression[] _set;

        public AndExpression(params IExpression[] set)
        {
            if (set != null)
            {
                _set = set.Where(s => s != null).ToArray();
            }
        }

        public void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (partition == null) throw new ArgumentNullException("partition");
            if (result == null) throw new ArgumentNullException("result");

            ushort itemCount = partition.Count;
            ShortSet expressionResults = null;
            ShortSet partResults = new ShortSet(itemCount);

            foreach (IExpression part in _set)
            {
                partResults.Clear();
                part.TryEvaluate(partition, partResults, details);

                if (expressionResults == null)
                {
                    expressionResults = new ShortSet(itemCount);
                    expressionResults.Or(partResults);
                }
                else
                {
                    expressionResults.And(partResults);
                }

                if (expressionResults.IsEmpty()) break;
            }

            result.Or(expressionResults);
        }

        public IList<IExpression> Children()
        {
            return _set;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (IExpression part in _set)
            {
                if (result.Length > 0) result.Append(" AND ");

                if (part is OrExpression)
                {
                    // Explicit parenthesis are only required on an OR expression inside and AND expression.
                    // AND takes precedence over OR, so A OR B AND C OR D => (A OR (B AND C) OR D)
                    result.Append("(");
                    result.Append(part);
                    result.Append(")");
                }
                else
                {
                    result.Append(part);
                }
            }

            return result.ToString();
        }
    }

    public class NotExpression : IExpression
    {
        // NOTE: Storing as array to allow replacement via Children property
        private IExpression[] _part;

        public NotExpression(IExpression part)
        {
            _part = new IExpression[] { part };
        }

        public void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");
            if (result == null) throw new ArgumentNullException("result");
            if (partition == null) throw new ArgumentNullException("partition");

            ushort itemCount = partition.Count;

            ShortSet partResults = new ShortSet(itemCount);
            _part[0].TryEvaluate(partition, partResults, details);
            partResults.Not();

            result.Or(partResults);
        }

        public IList<IExpression> Children()
        {
            return _part;
        }

        public override string ToString()
        {
            return StringExtensions.Format("NOT({0})", _part);
        }
    }

    public class EmptyExpression : IExpression
    {
        public static readonly IList<IExpression> EmptyEnumerable;

        static EmptyExpression()
        {
            EmptyEnumerable = new IExpression[0];
        }

        public EmptyExpression()
        { }

        public void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            // Nothing to add to result
        }

        public IList<IExpression> Children()
        {
            return EmptyEnumerable;
        }

        public override string ToString()
        {
            return String.Empty;
        }
    }

    public class AllExpression : IExpression
    {
        public void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");
            if (result == null) throw new ArgumentNullException("result");
            if (partition == null) throw new ArgumentNullException("partition");

            // Include all items - clear any and then add everything.
            // ShortSet will scope the set to the ID range valid within the data set.
            result.Clear();
            result.Not();
        }

        public IList<IExpression> Children()
        {
            return EmptyExpression.EmptyEnumerable;
        }

        public override string ToString()
        {
            // AllExpression is String.Empty [Aggregate results require this to identify totals]
            return "";
        }
    }

    public class TermExpression : IExpression
    {
        // The <Column> <operator> <Value> itself
        public string ColumnName;
        public Operator Operator;
        public Value Value;

        // Parsing details to drive IntelliSense.
        public IntelliSenseGuidance Guidance;

        public TermExpression(object value) : this("*", Operator.Matches, value)
        { }

        public TermExpression(string columnName, Operator op, object value)
        {
            this.ColumnName = columnName;
            this.Operator = op;
            this.Value = Value.Create(value);
        }

        public virtual void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");
            if (result == null) throw new ArgumentNullException("result");
            if (partition == null) throw new ArgumentNullException("partition");

            if (this.ColumnName.Equals("*"))
            {
                // '*' queries succeed if any column succeeds
                bool succeeded = false;
                ExecutionDetails perColumnDetails = new ExecutionDetails();

                foreach (IColumn<object> column in partition.Columns.Values)
                {
                    perColumnDetails.Succeeded = true;
                    column.TryWhere(this.Operator, this.Value, result, perColumnDetails);
                    succeeded |= perColumnDetails.Succeeded;
                }

                details.Succeeded &= succeeded;

                // If no column succeeded, report the full errors
                if (!succeeded)
                {
                    details.Merge(perColumnDetails);
                }
            }
            else
            {
                if (!partition.ContainsColumn(this.ColumnName))
                {
                    details.AddError(ExecutionDetails.ColumnDoesNotExist, this.ColumnName);
                }
                else
                {
                    partition.Columns[this.ColumnName].TryWhere(this.Operator, this.Value, result, details);
                }
            }
        }

        public IList<IExpression> Children()
        {
            return EmptyExpression.EmptyEnumerable;
        }

        public override string ToString()
        {
            return StringExtensions.Format("{0}{1}{2}", QueryParser.WrapColumnName(this.ColumnName), this.Operator.ToSyntaxString(), QueryParser.WrapValue(this.Value));
        }
    }

    public class AllExceptColumnsTermExpression : TermExpression
    {
        public HashSet<string> RestrictedColumns;

        public AllExceptColumnsTermExpression(HashSet<string> restrictedColumns, Operator op, Value value) :
            base("*", op, value)
        {
            this.RestrictedColumns = restrictedColumns;
        }

        public AllExceptColumnsTermExpression(HashSet<string> restrictedColumns, TermExpression previousExpression) :
            this(restrictedColumns, previousExpression.Operator, previousExpression.Value)
        { }

        public override void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");
            if (result == null) throw new ArgumentNullException("result");
            if (partition == null) throw new ArgumentNullException("partition");

            // Run on every column *except* excluded ones
            bool succeeded = false;
            ExecutionDetails perColumnDetails = new ExecutionDetails();

            foreach (IColumn<object> column in partition.Columns.Values)
            {
                if (!this.RestrictedColumns.Contains(column.Name))
                {
                    perColumnDetails.Succeeded = true;
                    column.TryWhere(this.Operator, this.Value, result, perColumnDetails);
                    succeeded |= perColumnDetails.Succeeded;
                }
            }

            details.Succeeded &= succeeded;

            // If no column succeeded, report the full errors
            if (!succeeded)
            {
                details.Merge(perColumnDetails);
            }
        }
    }

    public class TermInExpression : IExpression
    {
        public string ColumnName;
        public Operator Operator;
        public Array Values;

        public TermInExpression(string columnName, Array values) : this(columnName, Operator.Equals, values)
        { }

        public TermInExpression(string columnName, Operator op, Array values)
        {
            this.ColumnName = columnName;
            this.Operator = op;
            this.Values = values;

            // If this is a "Matches" JOIN, split the values coming in into terms
            // which will each be matched.
            if (op == Operator.Matches || op == Operator.MatchesExact)
            {
                this.Values = GetUniqueTerms(values);
            }
        }

        private Array GetUniqueTerms(Array values)
        {
            HashSet<ByteBlock> uniqueValues = new HashSet<ByteBlock>();

            // Get every unique word split from every value in the array
            SetSplitter s = new SetSplitter();
            for (int i = 0; i < values.Length; ++i)
            {
                ByteBlock value = (ByteBlock)values.GetValue(i);
                foreach (Range r in s.Split(value).Ranges)
                {
                    if (r.Length > 0)
                    {
                        uniqueValues.Add(new ByteBlock(value.Array, r.Index, r.Length));
                    }
                }
            }

            // Convert the result to an array
            ByteBlock[] result = new ByteBlock[uniqueValues.Count];
            int j = 0;
            foreach (ByteBlock value in uniqueValues)
            {
                result[j] = value;
                j++;
            }

            return result;
        }

        public void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");
            if (result == null) throw new ArgumentNullException("result");
            if (partition == null) throw new ArgumentNullException("partition");

            if (!partition.ContainsColumn(this.ColumnName))
            {
                details.AddError(ExecutionDetails.ColumnDoesNotExist, this.ColumnName);
            }
            else
            {
                IColumn<object> column = partition.Columns[this.ColumnName];

                for (int i = 0; i < this.Values.Length; ++i)
                {
                    column.TryWhere(this.Operator, this.Values.GetValue(i), result, details);
                }
            }
        }

        public IList<IExpression> Children()
        {
            return EmptyExpression.EmptyEnumerable;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(QueryParser.WrapColumnName(this.ColumnName));
            result.Append(this.Operator.ToSyntaxString());
            result.Append("IN(");

            for (int i = 0; i < this.Values.Length; ++i)
            {
                if (i > 0) result.Append(", ");

                if (i > 4)
                {
                    result.AppendFormat("... [{0:n0}]", this.Values.Length);
                    break;
                }

                result.Append(QueryParser.WrapValue(this.Values.GetValue(i)));
            }

            result.Append(")");
            return result.ToString();
        }
    }
}
