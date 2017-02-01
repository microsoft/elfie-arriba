// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Arriba.Extensions;
using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Structures
{
    [TestClass]
    public class ValueTests
    {
        [TestMethod]
        public void Value_Basic()
        {
            // Null
            Assert.AreEqual("", TryAllConversions(null));

            // String
            Assert.AreEqual("string:simple, ByteBlock:simple", TryAllConversions("simple"));

            // byte[]
            Assert.AreEqual("string:System.Byte[], ByteBlock:simple", TryAllConversions(new byte[] { 115, 105, 109, 112, 108, 101 }));

            // ByteBlock
            Assert.AreEqual("string:simple, ByteBlock:simple", TryAllConversions((ByteBlock)"simple"));
            Assert.AreEqual("string:simple, ByteBlock:simple", TryAllConversions_ValueTypeReference((ByteBlock)"simple"));

            // Boolean / String, Number
            Assert.AreEqual("string:true, ByteBlock:true, bool:True", TryAllConversions("true"));
            Assert.AreEqual("string:False, ByteBlock:False, bool:False", TryAllConversions("False"));
            Assert.AreEqual("string:True, ByteBlock:True, bool:True", TryAllConversions(true));
            Assert.AreEqual("string:True, ByteBlock:True, bool:True", TryAllConversions_ValueTypeReference(true));
            Assert.AreEqual("string:False, ByteBlock:False, bool:False", TryAllConversions(false));
            Assert.AreEqual("string:False, ByteBlock:False, bool:False", TryAllConversions_ValueTypeReference(false));
            
            // Number / String, Boolean
            Assert.AreEqual("string:50, ByteBlock:50, TimeSpan:50.00:00:00, double:50, float:50, ulong:50, long:50, uint:50, int:50, ushort:50, short:50, byte:50", TryAllConversions("50"));
            Assert.AreEqual("string:50, ByteBlock:50, double:50, float:50, ulong:50, long:50, uint:50, int:50, ushort:50, short:50, byte:50", TryAllConversions(50));
            Assert.AreEqual("string:50, ByteBlock:50, double:50, float:50, ulong:50, long:50, uint:50, int:50, ushort:50, short:50, byte:50", TryAllConversions_ValueTypeReference(50));
            Assert.AreEqual("string:-50, ByteBlock:-50, TimeSpan:-50.00:00:00, double:-50, float:-50, long:-50, int:-50, short:-50", TryAllConversions("-50"));
            Assert.AreEqual("string:-50, ByteBlock:-50, double:-50, float:-50, long:-50, int:-50, short:-50", TryAllConversions(-50));
            Assert.AreEqual("string:-50, ByteBlock:-50, double:-50, float:-50, long:-50, int:-50, short:-50", TryAllConversions_ValueTypeReference(-50));
            Assert.AreEqual("string:1, ByteBlock:1, bool:True, double:1, float:1, ulong:1, long:1, uint:1, int:1, ushort:1, short:1, byte:1", TryAllConversions_ValueTypeReference(1));
            Assert.AreEqual("string:0, ByteBlock:0, bool:False, double:0, float:0, ulong:0, long:0, uint:0, int:0, ushort:0, short:0, byte:0", TryAllConversions_ValueTypeReference(0));

            // ByteBlock to Number
            Assert.AreEqual("string:50, ByteBlock:50, TimeSpan:50.00:00:00, double:50, float:50, ulong:50, long:50, uint:50, int:50, ushort:50, short:50, byte:50", TryAllConversions((ByteBlock)"50"));

            // Number Limits - ensure upconvert but don't downconvert; no unsigned representation for negative values
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}", double.MaxValue), TryAllConversions(double.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}", double.MaxValue), TryAllConversions_ValueTypeReference(double.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:3.40282346638529E+38, float:{0}", float.MaxValue), TryAllConversions(float.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:3.40282346638529E+38, float:{0}", float.MaxValue), TryAllConversions_ValueTypeReference(float.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:1.84467440737096E+19, float:1.844674E+19, ulong:{0}", ulong.MaxValue), TryAllConversions(ulong.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:1.84467440737096E+19, float:1.844674E+19, ulong:{0}", ulong.MaxValue), TryAllConversions_ValueTypeReference(ulong.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:9.22337203685478E+18, float:9.223372E+18, ulong:{0}, long:{0}", long.MaxValue), TryAllConversions(long.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:9.22337203685478E+18, float:9.223372E+18, ulong:{0}, long:{0}", long.MaxValue), TryAllConversions_ValueTypeReference(long.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:4.294967E+09, ulong:{0}, long:{0}, uint:{0}", uint.MaxValue), TryAllConversions(uint.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:4.294967E+09, ulong:{0}, long:{0}, uint:{0}", uint.MaxValue), TryAllConversions_ValueTypeReference(uint.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:2.147484E+09, ulong:{0}, long:{0}, uint:{0}, int:{0}", int.MaxValue), TryAllConversions(int.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:2.147484E+09, ulong:{0}, long:{0}, uint:{0}, int:{0}", int.MaxValue), TryAllConversions_ValueTypeReference(int.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}", ushort.MaxValue), TryAllConversions(ushort.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}", ushort.MaxValue), TryAllConversions_ValueTypeReference(ushort.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}, short:{0}", short.MaxValue), TryAllConversions(short.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}, short:{0}", short.MaxValue), TryAllConversions_ValueTypeReference(short.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}, short:{0}, byte:{0}", byte.MaxValue), TryAllConversions(byte.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}, short:{0}, byte:{0}", byte.MaxValue), TryAllConversions_ValueTypeReference(byte.MaxValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, bool:False, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}, short:{0}, byte:{0}", 0), TryAllConversions(0));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, bool:False, double:{0}, float:{0}, ulong:{0}, long:{0}, uint:{0}, int:{0}, ushort:{0}, short:{0}, byte:{0}", 0), TryAllConversions_ValueTypeReference(0));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, long:{0}, int:{0}, short:{0}", -1), TryAllConversions(-1));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, long:{0}, int:{0}, short:{0}", -1), TryAllConversions_ValueTypeReference(-1));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, long:{0}, int:{0}, short:{0}", short.MinValue), TryAllConversions(short.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:{0}, long:{0}, int:{0}, short:{0}", short.MinValue), TryAllConversions_ValueTypeReference(short.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:-2.147484E+09, long:{0}, int:{0}", int.MinValue), TryAllConversions(int.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}, float:-2.147484E+09, long:{0}, int:{0}", int.MinValue), TryAllConversions_ValueTypeReference(int.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:-9.22337203685478E+18, float:-9.223372E+18, long:{0}", long.MinValue), TryAllConversions(long.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:-9.22337203685478E+18, float:-9.223372E+18, long:{0}", long.MinValue), TryAllConversions_ValueTypeReference(long.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:-3.40282346638529E+38, float:{0}", float.MinValue), TryAllConversions(float.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:-3.40282346638529E+38, float:{0}", float.MinValue), TryAllConversions_ValueTypeReference(float.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}", double.MinValue), TryAllConversions(double.MinValue));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{0}", double.MinValue), TryAllConversions_ValueTypeReference(double.MinValue));

            // DateTime / String
            Assert.AreEqual("string:2013-09-25 00:00:00Z, ByteBlock:2013-09-25 00:00:00Z, DateTime:2013-09-25 00:00:00Z", TryAllConversions(new DateTime(2013, 09, 25, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual("string:2013-09-25 00:00:00Z, ByteBlock:2013-09-25 00:00:00Z, DateTime:2013-09-25 00:00:00Z", TryAllConversions_ValueTypeReference(new DateTime(2013, 09, 25, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual("string:2013-09-25, ByteBlock:2013-09-25, DateTime:2013-09-25 00:00:00Z", TryAllConversions("2013-09-25"));
            Assert.AreEqual("string:09/25/2013, ByteBlock:09/25/2013, DateTime:2013-09-25 00:00:00Z", TryAllConversions("09/25/2013"));

            // Guid / String
            Guid g = Guid.NewGuid();
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, Guid:{0}", g), TryAllConversions(g));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, Guid:{0}", g), TryAllConversions_ValueTypeReference(g));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, Guid:{0}", g), TryAllConversions(g.ToString()));

            // TimeSpan / String
            TimeSpan t = TimeSpan.FromSeconds(116);
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, TimeSpan:{0}", t), TryAllConversions(t));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, TimeSpan:{0}", t), TryAllConversions_ValueTypeReference(t));
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, DateTime:{1:u}, TimeSpan:{0}", t, DateTime.Today.Add(t)), TryAllConversions(t.ToString()));

            // Hex Number
            string hexNumber = "0xF";
            int asInt = int.Parse("F", System.Globalization.NumberStyles.HexNumber);
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{1}, float:{1}, ulong:{1}", hexNumber, asInt), TryAllConversions(hexNumber));

            string hexNumber2 = "0XFF";
            int asInt2 = int.Parse("FF", System.Globalization.NumberStyles.HexNumber);
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{1}, float:{1}, ulong:{1}", hexNumber2, asInt2), TryAllConversions(hexNumber2));

            string hexNumber3 = "0x0FF0";
            int asInt3 = int.Parse("FF0", System.Globalization.NumberStyles.HexNumber);
            Assert.AreEqual(String.Format("string:{0}, ByteBlock:{0}, double:{1}, float:{1}, ulong:{1}", hexNumber3, asInt3), TryAllConversions(hexNumber3));
        }

        private static string TryAllConversions(object value)
        {
            Value v = Value.Create(value);

            List<string> results = new List<string>();

            string asString;
            if (v.TryConvert(out asString)) results.Add(String.Format("string:{0}", asString));

            ByteBlock asByteBlock;
            if (v.TryConvert(out asByteBlock)) results.Add(String.Format("ByteBlock:{0}", asByteBlock));

            DateTime asDateTime;
            if (v.TryConvert(out asDateTime)) results.Add(String.Format("DateTime:{0:u}", asDateTime));

            Guid asGuid;
            if (v.TryConvert(out asGuid)) results.Add(String.Format("Guid:{0}", asGuid));

            TimeSpan asTimeSpan;
            if (v.TryConvert(out asTimeSpan)) results.Add(String.Format("TimeSpan:{0}", asTimeSpan));

            bool asBool;
            if (v.TryConvert(out asBool)) results.Add(String.Format("bool:{0}", asBool));

            double asDouble;
            if (v.TryConvert(out asDouble)) results.Add(String.Format("double:{0}", asDouble));

            float asFloat;
            if (v.TryConvert(out asFloat)) results.Add(String.Format("float:{0}", asFloat));

            ulong asULong;
            if (v.TryConvert(out asULong)) results.Add(String.Format("ulong:{0}", asULong));

            long asLong;
            if (v.TryConvert(out asLong)) results.Add(String.Format("long:{0}", asLong));

            uint asUInt;
            if (v.TryConvert(out asUInt)) results.Add(String.Format("uint:{0}", asUInt));

            int asInt;
            if (v.TryConvert(out asInt)) results.Add(String.Format("int:{0}", asInt));

            ushort asUShort;
            if (v.TryConvert(out asUShort)) results.Add(String.Format("ushort:{0}", asUShort));

            short asShort;
            if (v.TryConvert(out asShort)) results.Add(String.Format("short:{0}", asShort));

            byte asByte;
            if (v.TryConvert(out asByte)) results.Add(String.Format("byte:{0}", asByte));

            return String.Join(", ", results);
        }

        private static string TryAllConversions_ValueTypeReference<T>(T value) where T : IEquatable<T>
        {
            ValueTypeReference<T> vtr = new ValueTypeReference<T>(value);
            Value v = Value.Create(vtr);

            List<string> results = new List<string>();

            string asString;
            if (v.TryConvert(out asString)) results.Add(String.Format("string:{0}", asString));

            ByteBlock asByteBlock;
            if (v.TryConvert(out asByteBlock)) results.Add(String.Format("ByteBlock:{0}", asByteBlock));

            DateTime asDateTime;
            if (v.TryConvert(out asDateTime)) results.Add(String.Format("DateTime:{0:u}", asDateTime));

            Guid asGuid;
            if (v.TryConvert(out asGuid)) results.Add(String.Format("Guid:{0}", asGuid));

            TimeSpan asTimeSpan;
            if (v.TryConvert(out asTimeSpan)) results.Add(String.Format("TimeSpan:{0}", asTimeSpan));

            bool asBool;
            if (v.TryConvert(out asBool)) results.Add(String.Format("bool:{0}", asBool));

            double asDouble;
            if (v.TryConvert(out asDouble)) results.Add(String.Format("double:{0}", asDouble));

            float asFloat;
            if (v.TryConvert(out asFloat)) results.Add(String.Format("float:{0}", asFloat));

            ulong asULong;
            if (v.TryConvert(out asULong)) results.Add(String.Format("ulong:{0}", asULong));

            long asLong;
            if (v.TryConvert(out asLong)) results.Add(String.Format("long:{0}", asLong));

            uint asUInt;
            if (v.TryConvert(out asUInt)) results.Add(String.Format("uint:{0}", asUInt));

            int asInt;
            if (v.TryConvert(out asInt)) results.Add(String.Format("int:{0}", asInt));

            ushort asUShort;
            if (v.TryConvert(out asUShort)) results.Add(String.Format("ushort:{0}", asUShort));

            short asShort;
            if (v.TryConvert(out asShort)) results.Add(String.Format("short:{0}", asShort));

            byte asByte;
            if (v.TryConvert(out asByte)) results.Add(String.Format("byte:{0}", asByte));

            return String.Join(", ", results);
        }

        [TestMethod]
        public void Value_BestType()
        {
            Assert.AreEqual("DateTime", Value.Create("2014-01-01").BestType().Name);
            Assert.AreEqual("DateTime", Value.Create(new DateTime(2014, 01, 01)).BestType().Name);
            Assert.AreEqual("DateTime", Value.Create(new ValueTypeReference<DateTime>(new DateTime(2014, 01, 01))).BestType().Name);
            Assert.AreEqual("Guid", Value.Create(Guid.NewGuid()).BestType().Name);
            Assert.AreEqual("Guid", Value.Create(new ValueTypeReference<Guid>(Guid.NewGuid())).BestType().Name);
            Assert.AreEqual("Boolean", Value.Create("False").BestType().Name);
            Assert.AreEqual("Double", Value.Create((double)(float.MaxValue) * 1.01).BestType().Name);
            Assert.AreEqual("Double", Value.Create(new ValueTypeReference<double>((double)(float.MaxValue) * 1.01)).BestType().Name);
            Assert.AreEqual("Single", Value.Create((double)(float.MaxValue)).BestType().Name);
            Assert.AreEqual("Single", Value.Create(new ValueTypeReference<double>((double)(float.MaxValue))).BestType().Name);
            Assert.AreEqual("Single", Value.Create(0.0).BestType().Name);
            Assert.AreEqual("Single", Value.Create(new ValueTypeReference<double>(0.0)).BestType().Name);
            Assert.AreEqual("Single", Value.Create((double)(float.MinValue)).BestType().Name);
            Assert.AreEqual("Single", Value.Create(new ValueTypeReference<double>((double)(float.MinValue))).BestType().Name);
            Assert.AreEqual("Double", Value.Create((double)(float.MinValue) * 1.01).BestType().Name);
            Assert.AreEqual("Double", Value.Create(new ValueTypeReference<double>((double)(float.MinValue) * 1.01)).BestType().Name);

            Assert.AreEqual("UInt64", Value.Create((ulong)(long.MaxValue) + 1).BestType().Name);
            Assert.AreEqual("UInt64", Value.Create(new ValueTypeReference<ulong>((ulong)(long.MaxValue) + 1)).BestType().Name);
            Assert.AreEqual("Int64", Value.Create((ulong)long.MaxValue).BestType().Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<ulong>((ulong)long.MaxValue)).BestType().Name);
            Assert.AreEqual("Int64", Value.Create((ulong)int.MaxValue + 1).BestType().Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<ulong>((ulong)int.MaxValue + 1)).BestType().Name);
            Assert.AreEqual("Int32", Value.Create((ulong)int.MaxValue).BestType().Name);
            Assert.AreEqual("Int32", Value.Create(new ValueTypeReference<ulong>((ulong)int.MaxValue)).BestType().Name);
            Assert.AreEqual("Int32", Value.Create((ushort)0).BestType().Name);
            Assert.AreEqual("Int32", Value.Create(new ValueTypeReference<ushort>((ushort)0)).BestType().Name);
            Assert.AreEqual("Int32", Value.Create((byte)0).BestType().Name);
            Assert.AreEqual("Int32", Value.Create(new ValueTypeReference<byte>((byte)0)).BestType().Name);
            Assert.AreEqual("Int32", Value.Create((long)int.MinValue).BestType().Name);
            Assert.AreEqual("Int32", Value.Create(new ValueTypeReference<long>((long)int.MinValue)).BestType().Name);
            Assert.AreEqual("Int64", Value.Create((long)int.MinValue - 1).BestType().Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<long>((long)int.MinValue - 1)).BestType().Name);
            Assert.AreEqual("Int64", Value.Create(long.MinValue).BestType().Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<long>(long.MinValue)).BestType().Name);

            Assert.AreEqual("String", Value.Create("Something").BestType().Name);
            Assert.AreEqual("String", Value.Create("1.0.0.1").BestType().Name);
            Assert.AreEqual("String", Value.Create("13/13/1313").BestType().Name);
        }

        [TestMethod]
        public void Value_BestTypeWithSoFar()
        {
            // Maintain special types if new values match
            Assert.AreEqual("DateTime", Value.Create("2014-01-01").BestType(typeof(DateTime)).Name);
            Assert.AreEqual("Guid", Value.Create(Guid.NewGuid()).BestType(typeof(Guid)).Name);
            Assert.AreEqual("Guid", Value.Create(new ValueTypeReference<Guid>(Guid.NewGuid())).BestType(typeof(Guid)).Name);
            Assert.AreEqual("Boolean", Value.Create("False").BestType(typeof(bool)).Name);

            // Maintain numeric types for matches
            Assert.AreEqual("Double", Value.Create((double)(float.MaxValue) * 1.01).BestType(typeof(double)).Name);
            Assert.AreEqual("Double", Value.Create(new ValueTypeReference<double>((double)(float.MaxValue) * 1.01)).BestType(typeof(double)).Name);
            Assert.AreEqual("Single", Value.Create(float.MaxValue).BestType(typeof(float)).Name);
            Assert.AreEqual("Single", Value.Create(new ValueTypeReference<float>(float.MaxValue)).BestType(typeof(float)).Name);
            Assert.AreEqual("Int64", Value.Create(long.MaxValue).BestType(typeof(long)).Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<long>(long.MaxValue)).BestType(typeof(long)).Name);
            Assert.AreEqual("Int32", Value.Create(int.MaxValue).BestType(typeof(int)).Name);
            Assert.AreEqual("Int32", Value.Create(new ValueTypeReference<int>(int.MaxValue)).BestType(typeof(int)).Name);

            // Promote numeric types if needed            
            Assert.AreEqual("Double", Value.Create(float.MaxValue).BestType(typeof(double)).Name);
            Assert.AreEqual("Double", Value.Create(new ValueTypeReference<float>(float.MaxValue)).BestType(typeof(double)).Name);
            Assert.AreEqual("Double", Value.Create(double.MaxValue).BestType(typeof(float)).Name);
            Assert.AreEqual("Double", Value.Create(new ValueTypeReference<double>(double.MaxValue)).BestType(typeof(float)).Name);
            Assert.AreEqual("Int64", Value.Create(int.MaxValue).BestType(typeof(long)).Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<int>(int.MaxValue)).BestType(typeof(long)).Name);
            Assert.AreEqual("Int64", Value.Create(long.MaxValue).BestType(typeof(int)).Name);
            Assert.AreEqual("Int64", Value.Create(new ValueTypeReference<long>(long.MaxValue)).BestType(typeof(int)).Name);

            // Floating point and integer turn into string
            Assert.AreEqual("String", Value.Create(float.MaxValue).BestType(typeof(int)).Name);
            Assert.AreEqual("String", Value.Create(new ValueTypeReference<float>(float.MaxValue)).BestType(typeof(int)).Name);
            Assert.AreEqual("String", Value.Create(double.MaxValue).BestType(typeof(long)).Name);
            Assert.AreEqual("String", Value.Create(new ValueTypeReference<double>(double.MaxValue)).BestType(typeof(long)).Name);

            // Other combinations turn into string
            Assert.AreEqual("String", Value.Create(Guid.NewGuid()).BestType(typeof(long)).Name);
            Assert.AreEqual("String", Value.Create(new ValueTypeReference<Guid>(Guid.NewGuid())).BestType(typeof(long)).Name);
            Assert.AreEqual("String", Value.Create(long.MaxValue).BestType(typeof(Guid)).Name);
            Assert.AreEqual("String", Value.Create(new ValueTypeReference<double>(long.MaxValue)).BestType(typeof(Guid)).Name);
            Assert.AreEqual("String", Value.Create("2014-01-01").BestType(typeof(bool)).Name);
            Assert.AreEqual("String", Value.Create(true).BestType(typeof(double)).Name);
            Assert.AreEqual("String", Value.Create(new ValueTypeReference<bool>(true)).BestType(typeof(double)).Name);
            Assert.AreEqual("String", Value.Create(ulong.MaxValue).BestType(typeof(int)).Name);
            Assert.AreEqual("String", Value.Create(new ValueTypeReference<ulong>(ulong.MaxValue)).BestType(typeof(int)).Name);
        }

        [TestMethod]
        public void Value_DoubleWrappingPrevention()
        {
            int literal = 12345;

            // Wrap the literal with Value.Create
            Value v1 = Value.Create(literal);
            int hashCode = v1.GetHashCode();

            // Wrap an already wrapped copy with Value.Create
            Value v2 = Value.Create(v1);
            Assert.AreEqual(v2.GetHashCode(), hashCode, "Value.Create re-wrap inconsistent with Value.Create");

            // Wrap the literal with Value.Assign
            Value v3 = Value.Create(null);
            v3.Assign(literal);
            Assert.AreEqual(v3.GetHashCode(), hashCode, "Value.Assign of literal inconsistent with Value.Create");

            // Wrap an already wrapped copy with Value.Assign
            Value v4 = Value.Create(null);
            v4.Assign(v3);
            Assert.AreEqual(v4.GetHashCode(), hashCode, "Value.Assign re-wrap inconsistent with Value.Create");

            // Wrap literal with Value.Create of ValueTypeReference
            Value v5 = Value.Create(new ValueTypeReference<int>(literal));
            Assert.AreEqual(v5.GetHashCode(), hashCode, "Value.Create with ValueTypeReference inconsistent with Value.Create");

            // Wrap an already wrapped copy with Value.Create
            Value v6 = Value.Create(v5);
            Assert.AreEqual(v6.GetHashCode(), hashCode, "Value.Create re-wrap of ValueTypeReference inconsistent with Value.Create");

            // Wrap literal with Value.Create of ValueTypeReference
            Value v7 = Value.Create(null);
            v7.Assign(new ValueTypeReference<int>(literal));
            Assert.AreEqual(v7.GetHashCode(), hashCode, "Value.Assign with ValueTypeReference inconsistent with Value.Create");

            // Wrap an already wrapped copy with Value.Create
            Value v8 = Value.Create(null);
            v8.Assign(v7);
            Assert.AreEqual(v8.GetHashCode(), hashCode, "Value.Assign re-wrap of ValueTypeReference inconsistent with Value.Create");

            // Wrap in a ValueTypeReferece<object>
            Value v9 = Value.Create(null);
            v9.Assign(new ValueTypeReference<object>(literal));
            Assert.AreEqual(v9.GetHashCode(), hashCode, "Value.Assign re-wrap of ValueTypeReference inconsistent with Value.Create");
        }

        [TestMethod]
        public void Value_ValueTypeReferenceEquivalence()
        {
            TryEquivalence(0);
            TryEquivalence(12345);
            TryEquivalence(true);
            TryEquivalence(false);
            TryEquivalence((short)12345);
            TryEquivalence((ushort)12345);
            TryEquivalence((long)12345);
            TryEquivalence((ulong)12345);
            TryEquivalence(new DateTime(2013, 09, 25));
        }

        private void TryEquivalence<T>(T testValue) where T : IEquatable<T>
        {
            Value v1 = Value.Create(testValue);
            Value v2 = Value.Create(testValue);
            Value v3 = Value.Create(new ValueTypeReference<T>(testValue));
            Value v4 = Value.Create(new ValueTypeReference<T>(testValue));

            Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode(), "Hash for Value and Value not equivelent for type: " + typeof(T));
            Assert.AreEqual(v1.GetHashCode(), v3.GetHashCode(), "Hash for Value and ValueTypeReference not equivelent for type: " + typeof(T));
            Assert.AreEqual(v3.GetHashCode(), v4.GetHashCode(), "Hash for ValueTypeReference and ValueTypeReference not equivelent for type: " + typeof(T));
            Assert.AreEqual(v1, v2, "Value and Value are not equivelent for type: " + typeof(T));
            Assert.AreEqual(v1, v3, "Value and ValueTypeReference are not equivelent for type: " + typeof(T));
            Assert.AreEqual(v3, v4, "ValueTypeReference and ValueTypeReference are not equivelent for type: " + typeof(T));
        }

        [TestMethod]
        public void Value_HashCheck()
        {
            byte partitionBits = 1;

            int id = 1253432;

            // Get Value, Hash, and Partition Index
            Value idValue = Value.Create(id);
            int hash = idValue.GetHashCode();
            int partition = PartitionMask.IndexOfHash(hash, partitionBits);

            // Verify Matches reports consistently with IndexOfHash
            PartitionMask mask = PartitionMask.BuildSet(partitionBits)[partition];
            string maskName = mask.ToString();
            Assert.IsTrue(mask.Matches(hash));

            // Verify Value.Assign hash is consistent
            Value n = Value.Create(null);
            n.Assign(id);
            int hashViaAssign = n.GetHashCode();
            Assert.AreEqual(hash, hashViaAssign);

            // Get Hash of unwrapped value [debuggability]
            int wrongHash = id.GetHashCode();
            int wrongPartition = PartitionMask.IndexOfHash(wrongHash, partitionBits);
        }

        [TestMethod]
        public void Value_HashDistribution()
        {
            int itemCount = 100 * 1000;
            byte bitCount = (byte)Math.Max(Math.Ceiling(Math.Log(itemCount, 2)) - 16, 0);
            int partitionCount = 1 << bitCount;

            int[] countPerPartition = new int[partitionCount];

            for (int i = 0; i < itemCount; ++i)
            {
                Value v = Value.Create(String.Format("Table.{0}", i));
                int partitionIndex = PartitionMask.IndexOfHash(v.GetHashCode(), bitCount);
                countPerPartition[partitionIndex]++;
            }

            int minCount = countPerPartition.Min();
            int maxCount = countPerPartition.Max();
            int difference = maxCount - minCount;

            Assert.IsTrue(difference / (float)minCount < 0.05, "Distribution of items was not very even.");
        }

#if !DEBUG
        [TestMethod]
#endif
        public void Value_Performance()
        {
            // Goal: >1M conversions per second. This sample has 9 values and 12 total conversions.
            DateTime now = DateTime.UtcNow;
            string nowString = now.ToString();

            ByteBlock blockIn = "two";

            ByteBlock blockOut;
            ulong ulongOut;
            bool boolOut;
            DateTime dateTimeOut;
            Type bestType;

            Stopwatch w = Stopwatch.StartNew();

            int iterations = 1000 * 1000;
            for (int i = 0; i < iterations; ++i)
            {
                Value v1 = Value.Create("one");
                v1.TryConvert(out blockOut);

                Value v2 = Value.Create(blockIn);
                v2.TryConvert(out blockOut);

                Value v3 = Value.Create("three");
                v3.TryConvert(out blockOut);

                Value v4 = Value.Create(14211);
                v4.TryConvert(out blockOut);
                v4.TryConvert(out ulongOut);

                Value v5 = Value.Create("14211");
                v5.TryConvert(out blockOut);
                v5.TryConvert(out ulongOut);

                Value v6 = Value.Create(true);
                v6.TryConvert(out boolOut);
                v6.TryConvert(out blockOut);

                Value v7 = Value.Create("false");
                v7.TryConvert(out boolOut);

                Value v8 = Value.Create(now);
                v8.TryConvert(out dateTimeOut);

                Value v9 = Value.Create(nowString);
                v9.TryConvert(out dateTimeOut);
                bestType = v9.BestType();
            }

            w.Stop();
            Trace.WriteLine(String.Format("{0:n0} iterations in {1}", iterations, w.Elapsed.ToFriendlyString()));
            double conversionsPerMillisecond = 12 * iterations / w.ElapsedMilliseconds;
            Assert.IsTrue(conversionsPerMillisecond > 1000, "Performance is not within goal");
        }
    }
}
