// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test.Serialization
{
    [TestClass]
    public class JsonTabularWriterTests
    {
        [TestMethod]
        public void JsonTabularWriter_Escaping()
        {
            StringBuilder allEscapedValues = new StringBuilder();

            // All 0x00 - 0x1F are escaped
            for (int i = 0; i < 32; ++i)
            {
                allEscapedValues.Append((char)i);
            }

            // Backslash is escaped
            allEscapedValues.Append('\\');

            // Quote is escaped
            allEscapedValues.Append('"');

            string all = allEscapedValues.ToString();
            String8 all8 = String8.Convert(all, new byte[String8.GetLength(all)]);

            using (ITabularWriter w = new JsonTabularWriter("Escaping.json"))
            {
                w.SetColumns(new string[] { "Bad" });
                w.Write(all8);
                w.NextRow();
            }

            string content = File.ReadAllText("Escaping.json");
            Assert.IsTrue(content.IndexOf("\\u0000\\u0001\\u0002\\u0003\\u0004\\u0005\\u0006\\u0007\\u0008\\u0009\\u000A\\u000B\\u000C\\u000D\\u000E\\u000F\\u0010\\u0011\\u0012\\u0013\\u0014\\u0015\\u0016\\u0017\\u0018\\u0019\\u001A\\u001B\\u001C\\u001D\\u001E\\u001F\\\\\\\"") >= 0);
        }
    }
}
