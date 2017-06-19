// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Arriba.Model.Column;


namespace Arriba.Types
{
    [DataContract]
    public class TableInformation
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime LastWriteTimeUtc { get; set; }

        [DataMember]
        public int PartitionCount { get; set; }

        [DataMember]
        public uint RowCount { get; set; }

        [DataMember]
        public IEnumerable<ColumnDetails> Columns { get; set; }

        [DataMember]
        public bool CanWrite { get; set; }

        [DataMember]
        public bool CanAdminister { get; set; }
    }
}
