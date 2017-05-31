// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class IpRangeColumnTests
    {
        [TestMethod]
        public void IpRange_Parsing()
        {
            // Full single IP
            Assert.AreEqual("10.1.15.230", ParseAndToString("10.1.15.230"));

            // Partial IPs -> Ranges
            Assert.AreEqual("10.1.15.0-10.1.15.255", ParseAndToString("10.1.15"));
            Assert.AreEqual("10.0.0.0-10.255.255.255", ParseAndToString("10"));
            Assert.AreEqual("10.0.0.0-10.255.255.255", ParseAndToString("10.*"));
            Assert.AreEqual("10.1.0.0-10.1.255.255", ParseAndToString("10.1.*"));
            Assert.AreEqual("0.0.0.0-255.255.255.255", ParseAndToString("*"));

            // IP Ranges
            Assert.AreEqual("10.1.15.10-10.1.15.200", ParseAndToString("10.1.15.10-10.1.15.200"));
            Assert.AreEqual("192.168.254.6-192.168.254.64", ParseAndToString("192.168.254.6-192.168.254.64"));
            Assert.AreEqual("10.1.15.230", ParseAndToString("10.1.15.230-10.1.15.230"));

            // CIDR Ranges
            Assert.AreEqual("10.11.0.0-10.11.255.255", ParseAndToString("10.11.0.0/16"));
            Assert.AreEqual("192.168.100.0-192.168.103.255", ParseAndToString("192.168.100.0/22"));
            Assert.AreEqual("192.168.100.0-192.168.100.255", ParseAndToString("192.168.100.15/24"));

            // Null, Empty, text
            Assert.AreEqual("0.0.0.0", ParseAndToString(null));
            Assert.AreEqual("0.0.0.0", ParseAndToString(""));
            Assert.AreEqual("", ParseAndToString("Sophie"));

            // Number but out of range
            Assert.AreEqual("", ParseAndToString("333"));
            Assert.AreEqual("", ParseAndToString("10.1.256.255"));
            Assert.AreEqual("", ParseAndToString("10.1.10.1.10"));

            // Bad ranges
            Assert.AreEqual("", ParseAndToString("192.168.254.6-192.168.254.5"));
            Assert.AreEqual("", ParseAndToString("192.168.254-192.168.256"));
        }

        private string ParseAndToString(string ipRange)
        {
            IpRange result;
            if (IpRange.TryParse(ipRange, out result))
            {
                return result.ToString();
            }

            return "";
        }

        [TestMethod]
        public void IpRangeColumn_Basic()
        {
            // Test the IP Range column with two distinct, valid sample values
            ColumnTests.ColumnTest_Basics(() => new IpRangeColumn(), "10.11.0.0", "192.168.100.0-192.168.103.255");
        }

        [TestMethod]
        public void ByteBlockColumn_TryWhere()
        {
            IpRangeColumn col = new IpRangeColumn();
            col.SetSize(4);
            col[0] = "10.0.0.0";
            col[1] = "10.0.0.*";
            col[2] = "192.168.1.60-192.168.1.80";
            col[3] = "192.168.100";
            col.Commit();

            Assert.AreEqual(null, GetMatches(col, Operator.Matches, "halo"), "Should error - not convertible to IPRange");
            Assert.AreEqual(null, GetMatches(col, Operator.StartsWith, "192.168.100"), "Should error - unsupported operator");

            Assert.AreEqual("[]", GetMatches(col, Operator.Matches, "1.*"), "Should have no matches - less than all ranges");
            Assert.AreEqual("[]", GetMatches(col, Operator.Matches, "193.*"), "Should have no matches - greater than all ranges");
            Assert.AreEqual("[2, 3]", GetMatches(col, Operator.Matches, "192.168"), "Should match 192.168 ranges");
            Assert.AreEqual("[2]", GetMatches(col, Operator.Matches, "192.168.0.0-192.168.1.60"), "Should match 192.168.1 range");
            Assert.AreEqual("[2]", GetMatches(col, Operator.Matches, "192.168.1.80-192.168.1.85"), "Should match 192.168.1 range");
            Assert.AreEqual("[]", GetMatches(col, Operator.Matches, "192.168.1.81-192.168.99.255"), "Should match no ranges (just outside)");

            Assert.AreEqual("[2]", GetMatches(col, Operator.Equals, "192.168.1.60-192.168.1.80"), "Matches (exact)");
            Assert.AreEqual("[]", GetMatches(col, Operator.Equals, "192.168.1.161-192.168.1.180"), "No Matches (off)");
            Assert.AreEqual("[]", GetMatches(col, Operator.Equals, "192.168.1.160-192.168.1.179"), "No Matches (off)");
            Assert.AreEqual("[]", GetMatches(col, Operator.Equals, "192.168.1.159-192.168.1.180"), "No Matches (off)");
            Assert.AreEqual("[]", GetMatches(col, Operator.Equals, "192.168.1.160-192.168.1.181"), "No Matches (off)");

            Assert.AreEqual("[2]", GetMatches(col, Operator.MatchesExact, "192.168.1.60-192.168.1.80"), "Matches (exact)");
            Assert.AreEqual("[]", GetMatches(col, Operator.MatchesExact, "192.168.1.160-192.168.1.181"), "No Matches (off)");

            Assert.AreEqual("[0, 1, 3]", GetMatches(col, Operator.NotEquals, "192.168.1.60-192.168.1.80"), "Matches except one");

            Assert.AreEqual("[]", GetMatches(col, Operator.LessThan, "10.0.0.0"), "Should match no ranges (just outside)");
            Assert.AreEqual("[0]", GetMatches(col, Operator.LessThan, "10.0.0.1"), "Should match 10.0.0.0 only (the other range overlaps)");
            Assert.AreEqual("[]", GetMatches(col, Operator.LessThan, "10.0.0.0-10.0.0.1"), "Should match no ranges (must be less than start)");
            Assert.AreEqual("[0, 1]", GetMatches(col, Operator.LessThan, "10.0.1.0"), "Should match 10.0 ranges");
            Assert.AreEqual("[0, 1, 2, 3]", GetMatches(col, Operator.LessThan, "192.168.101.0"), "Should match all ranges");

            Assert.AreEqual("[]", GetMatches(col, Operator.GreaterThan, "192.168.100.0"), "Should match no ranges (just outside)");
            Assert.AreEqual("[3]", GetMatches(col, Operator.GreaterThan, "192.168.99.255"), "Should match 192.168.100.* only");
            Assert.AreEqual("[]", GetMatches(col, Operator.GreaterThan, "192.168.99.255-192.168.100.0"), "Should match no ranges (must be greater than end)");
            Assert.AreEqual("[2, 3]", GetMatches(col, Operator.GreaterThan, "192.168.1.0-192.168.1.59"), "Should match 192.168 ranges");
            Assert.AreEqual("[0, 1, 2, 3]", GetMatches(col, Operator.GreaterThan, "9.*"), "Should match all ranges");

            Assert.AreEqual("[]", GetMatches(col, Operator.LessThanOrEqual, "9.255.255.255"), "Should match no ranges (just outside)");
            Assert.AreEqual("[0]", GetMatches(col, Operator.LessThanOrEqual, "10.0.0.0"), "Should match 10.0.0.0 (the other isn't fully in range)");
            Assert.AreEqual("[0, 1]", GetMatches(col, Operator.LessThanOrEqual, "10.0.0"), "Should match 10.0 ranges");

            Assert.AreEqual("[]", GetMatches(col, Operator.GreaterThanOrEqual, "192.168.101"), "Should match no ranges (just outside)");
            Assert.AreEqual("[3]", GetMatches(col, Operator.GreaterThanOrEqual, "192.168.100"), "Should match identical range");
            Assert.AreEqual("[2, 3]", GetMatches(col, Operator.GreaterThanOrEqual, "192.168.1.20-192.168.1.60"), "Should match both 192.168 ranges");
        }

        private static string GetMatches(IpRangeColumn col, Operator op, ByteBlock value)
        {
            ExecutionDetails details = new ExecutionDetails();
            ShortSet result = new ShortSet(col.Count);
            col.TryWhere(op, value, result, details);

            if (!details.Succeeded) return null;

            return result.ToString();
        }
    }
}
