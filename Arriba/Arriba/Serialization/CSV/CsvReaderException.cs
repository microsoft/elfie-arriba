// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Serialization.Csv
{
    /// <summary>
    /// Represents errors that occur when reading CSV files. 
    /// </summary>
    [Serializable]
    public class CsvReaderException : Exception
    {
        public CsvReaderException() { }
        public CsvReaderException(string message) : base(message) { }
        public CsvReaderException(string message, Exception inner) : base(message, inner) { }
        protected CsvReaderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
