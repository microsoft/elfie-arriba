// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Test.Extensions
{
    internal class SampleSerializable : IBinarySerializable
    {
        public int Value { get; set; }

        public SampleSerializable()
        {
            this.Value = 0;
        }

        public SampleSerializable(int value)
        {
            this.Value = value;
        }

        public static implicit operator SampleSerializable(int value)
        {
            return new SampleSerializable(value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SampleSerializable o = obj as SampleSerializable;
            if (o == null) return false;
            return this.Value.Equals(o.Value);
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }

        public void ReadBinary(BinaryReader r)
        {
            this.Value = r.ReadInt32();
        }

        public void WriteBinary(BinaryWriter w)
        {
            // Implement "Don't Write" condition
            if (this.Value > 0)
            {
                w.Write(this.Value);
            }
        }
    }
}
