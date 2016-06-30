// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class ValueTypeColumnTests
    {
        [TestMethod]
        public void ValueTypeColumn_Basic()
        {
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<int>(-1), -5, 10);
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<ushort>(ushort.MaxValue), (ushort)7, (ushort)3);
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<DateTime>(DateTime.MinValue), DateTime.UtcNow, DateTime.UtcNow.AddDays(3));

            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<int>(-1, ArrayExtensions.MinimumSize + 1), -5, 10);
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<ushort>(ushort.MaxValue, ArrayExtensions.MinimumSize + 1), (ushort)7, (ushort)3);
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<DateTime>(DateTime.MinValue, ArrayExtensions.MinimumSize + 1), DateTime.UtcNow, DateTime.UtcNow.AddDays(3));

            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<int>(-1, ushort.MaxValue), -5, 10);
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<ushort>(ushort.MaxValue, ushort.MaxValue), (ushort)7, (ushort)3);
            ColumnTests.ColumnTest_Basics(() => new ValueTypeColumn<DateTime>(DateTime.MinValue, ushort.MaxValue), DateTime.UtcNow, DateTime.UtcNow.AddDays(3));
        }
    }
}
