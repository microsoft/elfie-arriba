// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Extensions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model.Column
{
    public class ColumnDetails : IBinarySerializable
    {
        public const string UnknownType = "Unknown";

        public string Name { get; set; }
        public string Type { get; set; }
        public string Alias { get; set; }
        public object Default { get; set; }
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        ///  Serialization-only constructor
        /// </summary>
        internal ColumnDetails() : this(String.Empty)
        { }

        /// <summary>
        ///  Constructor from name only. Usable for providing a DataBlock for AddOrUpdate
        ///  but not enough information to create a column.
        /// </summary>
        /// <param name="name">Name of column</param>
        public ColumnDetails(string name)
            : this(name, UnknownType, null, String.Empty, false)
        { }

        /// <summary>
        ///  Constructor from name/type/default. Usable for creating a column.
        /// </summary>
        /// <param name="name">Name of column</param>
        /// <param name="type">TypeDescriptor of column</param>
        /// <param name="defaultValue">Default value for new items in column</param>
        public ColumnDetails(string name, string type, object defaultValue) : this(name, type, defaultValue, String.Empty, false)
        { }

        /// <summary>
        ///  Constructor from name/type/default. Usable for creating a column.
        /// </summary>
        /// <param name="name">Name of column</param>
        /// <param name="type">TypeDescriptor of column</param>
        /// <param name="defaultValue">Default value for new items in column</param>
        /// <param name="alias">Alternate name for column (if any)</param>
        /// <param name="isPrimaryKey">Whether this is the primary key column (there must be exactly one)</param>
        public ColumnDetails(string name, string type, object defaultValue, string alias, bool isPrimaryKey)
        {
            this.Name = name;
            this.Type = type;
            this.Alias = alias;
            this.Default = defaultValue;
            this.IsPrimaryKey = isPrimaryKey;
        }

        public static IList<ColumnDetails> FromNames(IEnumerable<string> columnNames)
        {
            if (columnNames == null) throw new ArgumentNullException("columnNames");

            List<ColumnDetails> result = new List<ColumnDetails>();

            foreach (string columnName in columnNames)
            {
                result.Add(new ColumnDetails(columnName));
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is ColumnDetails)) return false;

            ColumnDetails other = (ColumnDetails)obj;
            if (!(this.Name.Equals(other.Name))) return false;
            if (!(this.Type.Equals(other.Type))) return false;
            if (!(this.Alias.Equals(other.Alias))) return false;
            if (!(this.IsPrimaryKey.Equals(other.IsPrimaryKey))) return false;
            if (!Value.Create(this.Default).Equals(Value.Create(other.Default))) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Type.GetHashCode();
        }

        public override string ToString()
        {
            return StringExtensions.Format("{{ name: {0}, type: {1}, alias: {2}, default: {3}, isPrimaryKey: {4} }}", this.Name, this.Type, this.Alias, this.Default ?? "null", this.IsPrimaryKey);
        }

        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.Name = context.Reader.ReadString();
            this.Type = context.Reader.ReadString();
            this.Alias = context.Reader.ReadString();
            this.Default = context.Reader.ReadString();
            this.IsPrimaryKey = context.Reader.ReadBoolean();
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.Writer.Write(this.Name);
            context.Writer.Write(this.Type);
            context.Writer.Write(this.Alias ?? String.Empty);
            context.Writer.Write((this.Default ?? String.Empty).ToString());
            context.Writer.Write(this.IsPrimaryKey);
        }
    }
}
