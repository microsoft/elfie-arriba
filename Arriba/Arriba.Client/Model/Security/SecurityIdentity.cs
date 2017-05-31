// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

using Arriba.Serialization;

namespace Arriba.Model.Security
{
    /// <summary>
    /// Represents a security identity. 
    /// </summary>
    [DataContract]
    public class SecurityIdentity : IBinarySerializable
    {
        // Serialization constructor
        public SecurityIdentity()
        { }

        public SecurityIdentity(IdentityScope type, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            else if (name.Length == 0)
            {
                throw new ArgumentException("Name must not be empty", "name");
            }

            this.Scope = type;
            this.Name = name.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the scope of the identity. 
        /// </summary>
        [DataMember]
        public IdentityScope Scope
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the unique name of the identity. 
        /// </summary>
        [DataMember]
        public string Name
        {
            get;
            private set;
        }

        public override int GetHashCode()
        {
            return Scope.GetHashCode() ^ this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SecurityIdentity other = obj as SecurityIdentity;

            if (other == null)
            {
                return false;
            }

            return other.Scope == this.Scope && String.Equals(other.Name, this.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static SecurityIdentity Create(IdentityScope type, string identity)
        {
            return new SecurityIdentity(type, identity);
        }

        public void ReadBinary(ISerializationContext context)
        {
            this.Scope = (IdentityScope)context.Reader.ReadByte();
            this.Name = context.Reader.ReadString();
        }

        public void WriteBinary(ISerializationContext context)
        {
            context.Writer.Write((byte)this.Scope);
            context.Writer.Write(this.Name);
        }
    }
}
