// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

using Arriba.Extensions;

namespace Arriba
{
    [Serializable]
    public class ArribaException : Exception
    {
        public ArribaException() { }
        public ArribaException(string message) : base(message) { }
        public ArribaException(string message, Exception inner) : base(message, inner) { }
        protected ArribaException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ArribaColumnAccessDeniedException : ArribaException
    {
        public ArribaColumnAccessDeniedException() { }
        public ArribaColumnAccessDeniedException(string message) : base(message) { }
        public ArribaColumnAccessDeniedException(string message, Exception inner) : base(message, inner) { }
        protected ArribaColumnAccessDeniedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ArribaWriteException : ArribaException
    {
        public const string MessageFormatString = "Unable to write item ID '{0}' column '{1}' value of '{2}'. See inner exception for details.";

        public ArribaWriteException(object itemId, string columnName, object value, Exception innerException)
            : this(StringExtensions.Format(MessageFormatString, itemId, columnName, value ?? "null"), innerException)
        { }

        public ArribaWriteException() { }
        public ArribaWriteException(string message) : base(message) { }
        public ArribaWriteException(string message, Exception inner) : base(message, inner) { }
        protected ArribaWriteException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TableNotFoundException : ArribaException
    {
        public TableNotFoundException() { }
        public TableNotFoundException(string message) : base(message) { }
        public TableNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected TableNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ColumnNotFoundException : ArribaException
    {
        public ColumnNotFoundException() { }
        public ColumnNotFoundException(string message) : base(message) { }
        public ColumnNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ColumnNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
