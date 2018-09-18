using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Xsv.Test
{
    [TestClass]
    public class XsvTests
    {
        [TestMethod]
        public void Xsv_HtmlInnerText()
        {
            HtmlEscapeAndVerify(string.Empty, string.Empty);
            HtmlEscapeAndVerify("<html>", string.Empty);
            HtmlEscapeAndVerify("<div class='interesting'>Content</div>", "Content");
            HtmlEscapeAndVerify("First <div class='interesting'>Content</div> <title />Last", "First Content Last");
            HtmlEscapeAndVerify("Ok<div class='unclosed hmm", "Ok");
        }

        private static void HtmlEscapeAndVerify(string htmlContent, string expectedEscapedValue)
        {
            String8Block block = new String8Block();

            using (StringLastValueWriter writer = new StringLastValueWriter())
            {
                Program.WriteHtmlEscaped(block.GetCopy(htmlContent), writer);
                Assert.AreEqual(expectedEscapedValue, writer.LastValue.ToString());
            }
        }
    }

    public class StringLastValueWriter : ITabularWriter
    {
        public int RowCountWritten { get; set; }
        public long BytesWritten => -1;

        private byte[] _convertBuffer;
        private String8Block _appendBlock;

        public String8 LastValue { get; private set; }

        public StringLastValueWriter()
        {
            _convertBuffer = new byte[40];
            _appendBlock = new String8Block();
        }

        public void Dispose()
        { }

        public void NextRow()
        {
            this.RowCountWritten++;
        }

        public void SetColumns(IEnumerable<string> columnNames)
        { }

        public void Write(String8 value)
        {
            LastValue = value;
        }

        public void Write(DateTime value)
        {
            Write(String8.FromDateTime(value, _convertBuffer, 0));
        }

        public void Write(int value)
        {
            Write(String8.FromInteger(value, _convertBuffer));
        }

        public void Write(bool value)
        {
            Write(String8.FromBoolean(value));
        }

        public void Write(byte value)
        {
            _convertBuffer[0] = value;
            Write(new String8(_convertBuffer, 0, 1));
        }

        public void Write(long value)
        {
            Write(String8.FromNumber(value, _convertBuffer, 0));
        }

        public void Write(double value)
        {
            Write(String8.FromNumber(value, _convertBuffer, 0));
        }

        public void WriteValueEnd()
        { }

        public void WriteValuePart(String8 part)
        {
            LastValue = _appendBlock.Concatenate(LastValue, String8.Empty, part);
        }

        public void WriteValuePart(DateTime value)
        {
            WriteValuePart(String8.FromDateTime(value, _convertBuffer, 0));
        }

        public void WriteValuePart(int part)
        {
            WriteValuePart(String8.FromInteger(part, _convertBuffer));
        }

        public void WriteValuePart(bool part)
        {
            WriteValuePart(String8.FromBoolean(part));
        }

        public void WriteValuePart(byte c)
        {
            _convertBuffer[0] = c;
            WriteValuePart(new String8(_convertBuffer, 0, 1));
        }

        public void WriteValueStart()
        {
            LastValue = String8.Empty;
            _appendBlock.Clear();
        }
    }
}
