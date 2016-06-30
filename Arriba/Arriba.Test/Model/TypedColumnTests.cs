// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class TypedColumnTests
    {
        [TestMethod]
        public void TypedColumn_TimeSpan_Basic()
        {
            IColumn<object> c = ColumnFactory.Build(new ColumnDetails("Duration", "TimeSpan", null), 0);
            c.SetSize(10);

            c[0] = Value.Create(TimeSpan.FromMinutes(1));
            c[1] = Value.Create("01:00:00");
            c[2] = Value.Create("00:00:01");
            c[3] = Value.Create("1");

            CommitIfRequired(c);
            ShortSet longTimes = new ShortSet(c.Count);
            c.TryWhere(Operator.GreaterThan, TimeSpan.FromSeconds(30), longTimes, null);
            Assert.AreEqual("0, 1, 3", String.Join(", ", longTimes.Values));
        }

        [TestMethod]
        public void TypedColumn_Boolean_Basic()
        {
            IColumn<object> c = ColumnFactory.Build(new ColumnDetails("IsDuplicate", "bool", true), 0);
            c.SetSize(5);

            c[0] = Value.Create(true);
            c[1] = Value.Create(false);
            c[2] = Value.Create("True");
            c[3] = Value.Create("false");

            CommitIfRequired(c);
            ShortSet set = new ShortSet(c.Count);

            // True - set and default
            set.Clear();
            c.TryWhere(Operator.Equals, "true", set, null);
            Assert.AreEqual("0, 2, 4", String.Join(", ", set.Values));

            // False - set
            set.Clear();
            c.TryWhere(Operator.Equals, "false", set, null);
            Assert.AreEqual("1, 3", String.Join(", ", set.Values));

            // False (Matches)
            set.Clear();
            c.TryWhere(Operator.Matches, "false", set, null);
            Assert.AreEqual("1, 3", String.Join(", ", set.Values));

            // Not False
            set.Clear();
            c.TryWhere(Operator.NotEquals, "false", set, null);
            Assert.AreEqual("0, 2, 4", String.Join(", ", set.Values));

            // Values works, *including if not asking for all items in order*
            bool[] values = (bool[])c.GetValues(new ushort[] { 3, 2, 1, 0 });
            Assert.AreEqual("False, True, False, True", String.Join(", ", values));
        }

        private static void CommitIfRequired(object o)
        {
            if (o is ICommittable)
            {
                (o as ICommittable).Commit();
            }
        }
    }
}
