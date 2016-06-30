// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Extensions
{
    [TestClass]
    public class IColumnExtensionsTests
    {
        [TestMethod]
        public void IColumnExtensions_FindComponent()
        {
            IColumn<object> column = ColumnFactory.Build(new ColumnDetails("sample", "string", null), 0);
            Assert.IsNotNull(column.FindComponent<IndexedColumn>());
            Assert.IsNotNull(column.FindComponent<UntypedColumn<ByteBlock>>());
            Assert.IsNotNull(column.FindComponent<ByteBlockColumn>());
            Assert.IsNotNull(column.FindComponent<SortedColumn<ByteBlock>>());
            Assert.IsNull(column.FindComponent<ValueTypeColumn<int>>());

            IColumn c2 = new ValueTypeColumn<short>(short.MinValue);
            Assert.IsNull(c2.FindComponent<SortedColumn<short>>());
            Assert.IsNotNull(c2.FindComponent<ValueTypeColumn<short>>());
        }
    }
}
