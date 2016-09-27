// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Extensions
{
    [TestClass]
    public class IReadOnlyListExtensionsTests
    {
        [TestMethod]
        public void IReadOnlyListExtensions_GetDefaultEnumerator()
        {
            List<int> l = new List<int>();
            l.Add(1);
            l.Add(2);
            l.Add(3);

            StringBuilder result = new StringBuilder();

            using (IEnumerator<int> enumerator = l.GetDefaultEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Length > 0) result.Append(", ");
                    result.Append(enumerator.Current.ToString());
                }

                Assert.AreEqual("1, 2, 3", result.ToString());
                result.Clear();
                enumerator.Reset();

                IEnumerator untyped = enumerator;
                while (untyped.MoveNext())
                {
                    if (result.Length > 0) result.Append(", ");
                    result.Append(untyped.Current.ToString());
                }

                Assert.AreEqual("1, 2, 3", result.ToString());
            }
        }
    }
}
