// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Model.Tree
{
    /// <summary>
    ///  Path8 wraps an element in a SymbolTree and knows how to write the path
    ///  from the root without any string creation, just like String8.
    /// </summary>
    public struct Path8 : IWriteableString
    {
        private StringStore _strings;
        private ItemTree _tree;
        private int _index;
        private byte _delimiter;
        private int _includeDepth;

        public Path8(StringStore strings, ItemTree tree, int index, char delimiter, int includeDepth = -1)
            : this(strings, tree, index, (byte)delimiter, includeDepth)
        {
            if ((ushort)delimiter >= 0x80) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));
        }

        internal Path8(StringStore strings, ItemTree tree, int index, byte delimiter, int includeDepth = -1)
        {
            _strings = strings;
            _tree = tree;
            _index = index;
            _delimiter = (byte)delimiter;
            _includeDepth = includeDepth;
        }

        public static Path8 Empty = new Path8(null, null, -1, (byte)' ', 0);

        public String8 Name
        {
            get
            {
                if (IsRoot) return String8.Empty;
                return _strings[_tree.GetNameIdentifier(_index)];
            }
        }

        public Path8 Parent
        {
            get
            {
                if (IsRoot) return Path8.Empty;

                int parentIndex = _tree.GetParent(_index);
                return new Path8(_strings, _tree, parentIndex, _delimiter, _includeDepth - 1);
            }
        }

        public bool IsRoot
        {
            get { return _index <= 0 || _includeDepth == 0; }
        }

        public bool IsEmpty
        {
            get { return this.IsRoot; }
        }

        public int Length
        {
            get
            {
                int length = 0;

                Path8 parent = this.Parent;
                if (!parent.IsRoot)
                {
                    length += parent.Length;
                    length += 1;
                }

                length += Name.Length;

                return length;
            }
        }

        public int WriteTo(byte[] buffer, int index)
        {
            int length = 0;

            Path8 parent = this.Parent;
            if (!parent.IsRoot)
            {
                length += parent.WriteTo(buffer, index + length);

                buffer[index + length] = _delimiter;
                length++;
            }

            length += Name.WriteTo(buffer, index + length);

            return length;
        }

        public int WriteTo(Stream stream)
        {
            int length = 0;

            Path8 parent = this.Parent;
            if (!parent.IsRoot)
            {
                length += Parent.WriteTo(stream);
                stream.WriteByte(_delimiter);
                length++;
            }

            length += Name.WriteTo(stream);

            return length;
        }

        public int WriteTo(TextWriter writer)
        {
            int length = 0;

            Path8 parent = this.Parent;
            if (!parent.IsRoot)
            {
                length += Parent.WriteTo(writer);
                writer.Write((char)_delimiter);
                length++;
            }

            length += Name.WriteTo(writer);

            return length;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            using (StringWriter writer = new StringWriter(result))
            {
                WriteTo(writer);
            }
            return result.ToString();
        }
    }
}
