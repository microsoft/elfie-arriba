// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Client
{
    [Serializable]
    public class ArribaClientException : Exception
    {
        public ArribaClientException() { }
        public ArribaClientException(string message) : base(message) { }
        public ArribaClientException(string message, Exception inner) : base(message, inner) { }
        protected ArribaClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
