// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

using Elfie.Test;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class ITabularValueTests
    {
        [TestMethod]
        public void ITabularValue_Basics()
        {
            // Null/Empty
            ITabularValue_Basics(null);
            ITabularValue_Basics(String.Empty);

            // Boolean
            ITabularValue_Basics("false");
            ITabularValue_Basics(false);

            // DateTime
            ITabularValue_Basics("2017-03-08");
            ITabularValue_Basics(new DateTime(2017, 03, 08, 0, 0, 0, DateTimeKind.Utc));

            // Integer and numeric conversions
            ITabularValue_Basics("-1");
            ITabularValue_Basics(-1);
            ITabularValue_Basics((byte)121);
            ITabularValue_Basics((sbyte)121);
            ITabularValue_Basics((short)121);
            ITabularValue_Basics((ushort)121);
            ITabularValue_Basics((int)121);
            ITabularValue_Basics((uint)121);
            ITabularValue_Basics((long)121);
            ITabularValue_Basics((ulong)121);
        }

        private static void ITabularValue_Basics(object value)
        {
            string valueString = null;
            if (value != null)
            {
                if (value is DateTime)
                {
                    valueString = ((DateTime)value).ToString("u");
                }
                else
                {
                    valueString = value.ToString();
                }
            }

            String8 value8 = String8.Convert(valueString, new byte[String8.GetLength(valueString) + 1], 1);

            String8TabularValue itv8 = new String8TabularValue();
            itv8.SetValue(value8);
            ITabularValue_Basics(valueString ?? "", value8, itv8);

            ObjectTabularValue otv = new ObjectTabularValue(new String8Block());
            otv.SetValue(value);
            ITabularValue_Basics(valueString, value8, otv);
        }

        private static void ITabularValue_Basics(string value, String8 value8, ITabularValue itv)
        {
            Assert.AreEqual(String.IsNullOrEmpty(value), itv.IsNullOrEmpty());
            Assert.AreEqual(value8, itv.ToString8());
            Assert.AreEqual(value, itv.ToString());

            bool asBoolean;
            if (bool.TryParse(value, out asBoolean))
            {
                Assert.AreEqual(asBoolean, itv.ToBoolean());
            }
            else
            {
                Verify.Exception<FormatException>(() => { var result = itv.ToBoolean(); });
            }

            int asInteger;
            if (int.TryParse(value, out asInteger))
            {
                Assert.AreEqual(asInteger, itv.ToInteger());
            }
            else
            {
                Verify.Exception<FormatException>(() => { var result = itv.ToInteger(); });
            }

            DateTime asDateTime;
            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out asDateTime))
            {
                Assert.AreEqual(asDateTime, itv.ToDateTime());
            }
            else
            {
                Verify.Exception<FormatException>(() => { var result = itv.ToDateTime(); });
            }
        }
    }
}
