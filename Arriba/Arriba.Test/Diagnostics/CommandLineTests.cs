// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test
{
    [TestClass]
    public class CommandLineTests
    {
        [TestMethod]
        public void CommandLine_Basics()
        {
            Assert.AreEqual("", CommandLine.Parse("").ToString());
            Assert.AreEqual("/mode:load", CommandLine.Parse("/mode:load").ToString());
            Assert.AreEqual("/name:\"First Document\"", CommandLine.Parse("/name:\"First Document\"").ToString());
            Assert.AreEqual("/quoted:\"I'm \"\"Quoted\"\" Here\"", CommandLine.Parse("/quoted:\"I'm \"\"Quoted\"\" Here\"").ToString());

            // Validate executable trimming
            Assert.AreEqual("/mode:import", CommandLine.Parse("Sample.exe /mode:import").ToString());
            Assert.AreEqual("/mode:import", CommandLine.Parse(@"""C:\Program Files (x86)\Microsoft\Sample.exe"" /mode:import").ToString());

            CommandLine c = CommandLine.Parse("/mode:import /table:Scratch /select:\"Date, Adj Close\" /take:20 /orderBy:Date /load:true");
            Assert.AreEqual("/mode:import /table:Scratch /select:\"Date, Adj Close\" /take:20 /orderBy:Date /load:true", c.ToString());

            // Get strings out, including quoted
            Assert.AreEqual("import", c.GetString("mode"));
            Assert.AreEqual("Date, Adj Close", c.GetString("select"));

            // Get an integer and bool out
            Assert.AreEqual(20, c.GetInt("take"));
            Assert.AreEqual(true, c.GetBool("load"));

            // Get unconvertible values (throw)
            Verify.Exception<ArgumentIsWrongTypeException>(() => c.GetInt("orderBy"), String.Format(ArgumentIsWrongTypeException.ArgumentIsWrongTypeMessage, "orderBy", "Date", "int"));
            Verify.Exception<ArgumentIsWrongTypeException>(() => c.GetBool("orderBy"), String.Format(ArgumentIsWrongTypeException.ArgumentIsWrongTypeMessage, "orderBy", "Date", "bool"));

            // Get missing values with defaults
            Assert.AreEqual("import", c.GetString("missing", "import"));
            Assert.AreEqual(30, c.GetInt("missing", 30));
            Assert.AreEqual(false, c.GetBool("missing", false));

            // Get missing values without defaults [throw]
            Verify.Exception<MissingRequiredArgumentException>(() => c.GetString("missing"), String.Format(MissingRequiredArgumentException.MissingArgumentMessage, "missing"));
            Verify.Exception<MissingRequiredArgumentException>(() => c.GetInt("missing"), String.Format(MissingRequiredArgumentException.MissingArgumentMessage, "missing"));
            Verify.Exception<MissingRequiredArgumentException>(() => c.GetBool("missing"), String.Format(MissingRequiredArgumentException.MissingArgumentMessage, "missing"));
        }
    }
}
