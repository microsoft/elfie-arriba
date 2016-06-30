// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

using Arriba.Model.Column;
using Arriba.Model.Security;

namespace Arriba.Types
{
    [DataContract]
    public class CreateTableRequest
    {
        public CreateTableRequest()
        {
            this.Columns = new List<ColumnDetails>();
            this.Permissions = new SecurityPermissions();
        }

        public CreateTableRequest(string tableName, long itemCountLimit) : this()
        {
            this.TableName = tableName;
            this.ItemCountLimit = itemCountLimit;
        }

        [DataMember]
        public string TableName
        {
            get;
            set;
        }

        [DataMember]
        public long ItemCountLimit
        {
            get;
            set;
        }

        [DataMember]
        public SecurityPermissions Permissions
        {
            get;
            set;
        }

        [DataMember]
        public ICollection<ColumnDetails> Columns { get; set; }
    }
}
