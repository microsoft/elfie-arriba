// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Structures;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    public interface IItemConsumer : IDisposable
    {
        /// <summary>
        ///  Create a table with the given columns and permisssions.
        /// </summary>
        /// <param name="columns">Columns to add</param>
        /// <param name="permissions">Permissions to set</param>
        void CreateTable(IList<ColumnDetails> columns, SecurityPermissions permissions);

        /// <summary>
        ///  Write the next block of new or changed items to this consumer.
        /// </summary>
        /// <param name="items">Block of items to write</param>
        void Append(DataBlock items);

        /// <summary>
        ///  Ensure changes are committed to the underlying datastore.
        /// </summary>
        void Save();
    }
}
