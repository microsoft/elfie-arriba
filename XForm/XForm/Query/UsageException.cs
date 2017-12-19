// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace XForm.Query
{
    public class ErrorContext
    {
        public string TableName { get; set; }
        public int QueryLineNumber { get; set; }
        public string Usage { get; set; }
        public string ErrorMessage { get; set; }
        public string InvalidValue { get; set; }
        public string InvalidValueCategory { get; set; }
        public IEnumerable<string> ValidValues { get; set; }

        public ErrorContext()
        { }

        public ErrorContext(string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
        {
            this.InvalidValue = invalidValue;
            this.InvalidValueCategory = invalidValueCategory;
            this.ValidValues = validValues;

            // Always sort expected values
            if (this.ValidValues != null) this.ValidValues = this.ValidValues.OrderBy((s) => s);
        }

        public ErrorContext Merge(ErrorContext inner)
        {
            if (inner != null)
            {
                if (!String.IsNullOrEmpty(inner.TableName)) TableName = inner.TableName;
                if (inner.QueryLineNumber > 0) QueryLineNumber = inner.QueryLineNumber;
                if (!String.IsNullOrEmpty(inner.Usage)) Usage = inner.Usage;
                if (!String.IsNullOrEmpty(inner.ErrorMessage)) ErrorMessage = inner.ErrorMessage;
                if (!String.IsNullOrEmpty(inner.InvalidValue)) InvalidValue = inner.InvalidValue;
                if (!String.IsNullOrEmpty(inner.InvalidValueCategory)) InvalidValueCategory = inner.InvalidValueCategory;
                if (inner.ValidValues != null) ValidValues = inner.ValidValues;
            }

            return this;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder();
            if (!String.IsNullOrEmpty(TableName)) message.AppendLine($"Table: {TableName}");
            if (QueryLineNumber > 0) message.AppendLine($"Line: {QueryLineNumber}");
            if (!String.IsNullOrEmpty(Usage)) message.AppendLine($"Usage: {Usage}");

            if (!String.IsNullOrEmpty(ErrorMessage))
            {
                message.AppendLine(ErrorMessage);
            }
            else
            {
                if (String.IsNullOrEmpty(InvalidValueCategory))
                {
                    message.AppendLine($"Value \"{InvalidValue}\" found when no more arguments were expected.");
                }
                else if (String.IsNullOrEmpty(InvalidValue))
                {
                    message.AppendLine($"No argument found when {InvalidValueCategory} was required.");
                }
                else
                {
                    message.AppendLine($"\"{InvalidValue}\" was not a valid {InvalidValueCategory}.");
                }
            }

            if (ValidValues != null)
            {
                message.AppendLine("Valid Options:");
                foreach (string value in ValidValues)
                {
                    message.AppendLine(value);
                }
            }

            return message.ToString();
        }
    }

    [Serializable]
    public class UsageException : ArgumentException
    {
        public ErrorContext Context { get; private set; }

        public UsageException(string invalidValue, string invalidValueCategory, IEnumerable<string> validValues)
            : this(new ErrorContext(invalidValue, invalidValueCategory, validValues))
        { }

        public UsageException(ErrorContext context)
            : base(context.ToString())
        {
            Context = context;
        }

        public UsageException(ErrorContext context, Exception innerException)
            : base(context.ToString(), innerException)
        {
            Context = context;
        }

        public UsageException() { }
        public UsageException(string message) : base(message) { }
        public UsageException(string message, Exception inner) : base(message, inner) { }
        protected UsageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
