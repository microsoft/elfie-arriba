// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Arriba.Server
{
    [DataContract]
    public class ArribaResponseEnvelope
    {
        public ArribaResponseEnvelope(bool success, object content)
        {
            this.Success = success;
            this.Content = content;
        }

        [DataMember]
        public object Content
        {
            get;
            set;
        }

        [DataMember]
        public bool Success
        {
            get;
            set;
        }

        [DataMember]
        public IDictionary<string, double> TraceTimings
        {
            get;
            set;
        }
    }

    [DataContract]
    public class ArribaResponseEnvelope<T>
    {
        public ArribaResponseEnvelope(bool success, T content)
        {
            this.Success = success;
            this.Content = content;
        }

        [DataMember]
        public T Content
        {
            get;
            set;
        }

        [DataMember]
        public bool Success
        {
            get;
            set;
        }

        [DataMember]
        public IDictionary<string, double> TraceTimings
        {
            get;
            set;
        }
    }
}
