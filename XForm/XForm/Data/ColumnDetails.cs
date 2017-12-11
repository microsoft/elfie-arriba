// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Data
{
    /// <summary>
    ///  ColumnDetails provides column metadata for columns in a DataBatch.
    ///  Most importantly, it includes the name (to find columns), the type
    ///  (to invoke typed operations), and whether it's nullable.
    /// </summary>
    public class ColumnDetails
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public bool Nullable { get; private set; }

        // Default? Unique? AutoIncrement? Constraint? IsIdentity? IsMeasure?

        public ColumnDetails(string name, Type type, bool nullable)
        {
            this.Name = name;
            this.Type = type;
            this.Nullable = nullable;
        }

        public ColumnDetails Rename(string name)
        {
            return new ColumnDetails(name, this.Type, this.Nullable);
        }

        public ColumnDetails ChangeType(Type type)
        {
            return new ColumnDetails(this.Name, type, this.Nullable);
        }

        public override bool Equals(object obj)
        {
            ColumnDetails other = obj as ColumnDetails;
            if (other == null) return false;

            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase)
                && this.Type.Equals(other.Type)
                && this.Nullable.Equals(other.Nullable);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Type.GetHashCode() ^ this.Nullable.GetHashCode();
        }
    }
}
