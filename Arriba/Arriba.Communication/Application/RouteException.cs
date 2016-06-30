// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Communication.Application
{
    [Serializable]
    public class RouteException : Exception
    {
        public RouteException() { }
        public RouteException(string message) : base(message) { }
        public RouteException(string message, Exception inner) : base(message, inner) { }
        protected RouteException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
