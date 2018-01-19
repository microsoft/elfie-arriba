// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Data
{
    /// <summary>
    ///  ColumnDetails provides column metadata for columns in an XArray.
    /// </summary>
    public class ColumnDetails
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }

        public ColumnDetails(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }

        public ColumnDetails Rename(string name)
        {
            return new ColumnDetails(name, this.Type);
        }

        public ColumnDetails ChangeType(Type type)
        {
            return new ColumnDetails(this.Name, type);
        }

        public override bool Equals(object obj)
        {
            ColumnDetails other = obj as ColumnDetails;
            if (other == null) return false;

            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase)
                && this.Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Type.GetHashCode();
        }
    }
}
