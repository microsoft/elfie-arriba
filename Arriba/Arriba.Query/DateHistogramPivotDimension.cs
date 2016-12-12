// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

using Arriba.Extensions;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    public class DateHistogramPivotDimension : PivotDimension
    {
        public DateHistogramPivotDimension() : base()
        { }

        public DateHistogramPivotDimension(string column) : base(column)
        { }

        public DateHistogramPivotDimension(string column, DateTime? from, DateTime? to, DateHistogramInterval interval)
            : base(column, StringExtensions.Format("DateTime Histogram [{0}] from '{1}' to '{2}' interval '{3}'", column, from, to, interval))
        {
            this.From = from;
            this.To = to;
            this.Interval = interval;
        }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public DateHistogramInterval? Interval { get; set; }

        internal override void CalculateClauses(ITable table, IExpression where)
        {
            DateTime from, to = DateTime.Now;

            // TODO: Handle inverse intervals 
            if (this.From == null)
            {
                if (!this.TryGetScalarValue<DateTime>(table, where, ScalarValueType.MinimumValue, out from))
                {
                    // Assume no dimensions as we failed to get a value
                    return;
                }
            }
            else
            {
                from = this.From.Value;
            }

            if (this.To == null)
            {
                if (!this.TryGetScalarValue<DateTime>(table, where, ScalarValueType.MaximumValue, out to))
                {
                    // Assume no dimensions as we failed to get a value
                    return;
                }
            }
            else
            {
                to = this.To.Value;
            }

            var interval = this.Interval ?? DateHistogramInterval.Month;

            if (interval == DateHistogramInterval.Day)
            {
                this.CalculateByDay(from, to);
            }
            else if (interval == DateHistogramInterval.Month)
            {
                this.CalculateByMonth(from, to);
            }
        }

        private void CalculateByMonth(DateTime from, DateTime to)
        {
            var current = FloorToMonth(from);
            to = AddMonth(to);

            while (current < to)
            {
                DateTime next = AddMonth(current);
                this.AddCondition(StringExtensions.Format("[{0}] >= {1} AND [{0}] < {2}", this.Column, current.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), next.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)));
                current = next;
            }
        }

        private void CalculateByDay(DateTime from, DateTime to)
        {
            to = to.Date + TimeSpan.FromDays(1);

            // Increment by day
            for (DateTime i = from; i < to; i += TimeSpan.FromDays(1))
            {
                DateTime next = i + TimeSpan.FromDays(1);
                this.AddCondition(StringExtensions.Format("[{0}] >= {1} AND [{0}] < {2}", this.Column, i.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), next.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)));
            }
        }

        private static DateTime AddMonth(DateTime datetime)
        {
            int year = datetime.Year;
            int month = datetime.Month + 1;

            if (month == 13) { month = 1; year++; }

            return new DateTime(year, month, 1);
        }

        private static DateTime FloorToMonth(DateTime datetime)
        {
            return new DateTime(datetime.Year, datetime.Month, 1);
        }
    }

    public enum DateHistogramInterval
    {
        Day,
        Month
    }
}
