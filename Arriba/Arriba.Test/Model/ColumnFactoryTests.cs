// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using Arriba.Model;
using Arriba.Model.Column;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Model
{
    [TestClass]
    public class ColumnFactoryTests
    {
        private string _defaultWrappingFormatString = "UntypedColumn<{0}>;FastAddSortedColumn<{0}>;ValueTypeColumn<{0}>";

        [TestCleanup]
        public void TestCleanup()
        {
            ColumnFactory.ResetColumnCreators();
        }

        [TestMethod]
        public void ColumnFactory_Basic()
        {
            string sortedColumnName = "FastAddSortedColumn";

            // Strings
            Assert.AreEqual(string.Format("UntypedColumn<ByteBlock>;IndexedColumn[HtmlWordSplitter];{0}<ByteBlock>;ByteBlockColumn", sortedColumnName), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "html", null), 0)));
            Assert.AreEqual(string.Format("UntypedColumn<ByteBlock>;IndexedColumn[DefaultWordSplitter];{0}<ByteBlock>;ByteBlockColumn", sortedColumnName), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "json", null), 0)));
            Assert.AreEqual(string.Format("UntypedColumn<ByteBlock>;IndexedColumn[DefaultWordSplitter];{0}<ByteBlock>;ByteBlockColumn", sortedColumnName), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "string", null), 0)));

            // Special Types
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Guid"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "guid", null), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "DateTime"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "DateTime", null), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "TimeSpan"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "TimeSpan", null), 0)));
            Assert.AreEqual("UntypedColumn<Boolean>;BooleanColumn", WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "bool", null), 0)));

            // Supported Numeric Types (both naming styles)
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Byte"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "byte", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Int16"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "short", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Int16"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "Int16", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Int32"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "int", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Int32"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "Int32", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Int64"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "long", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Int64"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "Int64", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "UInt64"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "ulong", -1), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "UInt64"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "UInt64", -1), 0)));

            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Single"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "Single", -1.0), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Single"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "float", -1.0), 0)));
            Assert.AreEqual(String.Format(_defaultWrappingFormatString, "Double"), WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "double", -1), 0)));
        }

        [TestMethod]
        public void ColumnFactory_BasicExtensibility()
        {
            CustomColumnSupport.RegisterCustomColumns();
            // Registering the same name twice throws an exception
            Verify.Exception<ArribaException>(() => CustomColumnSupport.RegisterCustomColumns());

            // Column factory can call custom column creators
            Assert.AreEqual("UntypedColumn<ComparableColor>;ColorColumn", WriteCompleteType(ColumnFactory.Build(new ColumnDetails("Unused", "color", null), 0)));
        }

        [TestMethod]
        public void ColumnFactory_CanonicalTypeName()
        {
            Assert.AreEqual("guid", ColumnFactory.GetCanonicalTypeName(typeof(Guid)));
            Assert.AreEqual("datetime", ColumnFactory.GetCanonicalTypeName(typeof(DateTime)));
            Assert.AreEqual("timespan", ColumnFactory.GetCanonicalTypeName(typeof(TimeSpan)));

            Assert.AreEqual("boolean", ColumnFactory.GetCanonicalTypeName(typeof(bool)));
            Assert.AreEqual("byte", ColumnFactory.GetCanonicalTypeName(typeof(byte)));
            Assert.AreEqual("sbyte", ColumnFactory.GetCanonicalTypeName(typeof(sbyte)));
            Assert.AreEqual("short", ColumnFactory.GetCanonicalTypeName(typeof(short)));
            Assert.AreEqual("ushort", ColumnFactory.GetCanonicalTypeName(typeof(ushort)));
            Assert.AreEqual("int", ColumnFactory.GetCanonicalTypeName(typeof(int)));
            Assert.AreEqual("uint", ColumnFactory.GetCanonicalTypeName(typeof(uint)));
            Assert.AreEqual("long", ColumnFactory.GetCanonicalTypeName(typeof(long)));
            Assert.AreEqual("ulong", ColumnFactory.GetCanonicalTypeName(typeof(ulong)));
            Assert.AreEqual("float", ColumnFactory.GetCanonicalTypeName(typeof(float)));
            Assert.AreEqual("double", ColumnFactory.GetCanonicalTypeName(typeof(double)));
            Assert.AreEqual("string", ColumnFactory.GetCanonicalTypeName(typeof(string)));
        }

        /// <summary>
        ///  Return a semicolon-delimited list of all of the column components including type parameters.
        ///  Ex: UntypedColumn&lt;Int32&gt;;SortedColumn&lt;Int32&gt;;ValueTypeColumn&lt;Int32&gt;
        /// </summary>
        /// <param name="column">Column to identify</param>
        /// <returns>String containing type name and type parameter name for each column component, semi-colon delimited</returns>
        private static string WriteCompleteType(IColumn column)
        {
            StringBuilder result = new StringBuilder();

            IColumn current = column;
            while (current != null)
            {
                if (result.Length > 0) result.Append(";");
                AppendTypeName(current.GetType(), result);

                if (current is IndexedColumn)
                {
                    result.Append("[");
                    AppendTypeName(((IndexedColumn)current).Splitter.GetType(), result);
                    result.Append("]");
                }

                current = current.InnerColumn;
            }

            return result.ToString();
        }

        private static void AppendTypeName(Type t, StringBuilder result)
        {
            result.Append(t.Name.Replace("`1", String.Empty));

            if (t.GenericTypeArguments.Length > 0)
            {
                result.Append("<");

                for (int i = 0; i < t.GenericTypeArguments.Length; ++i)
                {
                    if (i > 0) result.Append(", ");
                    AppendTypeName(t.GenericTypeArguments[i], result);
                }

                result.Append(">");
            }
        }
    }
}
