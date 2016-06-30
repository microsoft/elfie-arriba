// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Arriba.Serialization;

namespace Arriba.Model.Security
{
    /// <summary>
    /// Represents access permissions to a resource. 
    /// </summary>
    public class SecurityPermissions : IBinarySerializable
    {
        private HashSet<SecurityIdentity> _readers = new HashSet<SecurityIdentity>();
        private HashSet<SecurityIdentity> _writers = new HashSet<SecurityIdentity>();
        private HashSet<SecurityIdentity> _owners = new HashSet<SecurityIdentity>();

        public SecurityPermissions()
        {
        }

        public IEnumerable<SecurityIdentity> Readers
        {
            get { return _readers; }
        }

        public IEnumerable<SecurityIdentity> Writers
        {
            get { return _writers; }
        }

        public IEnumerable<SecurityIdentity> Owners
        {
            get { return _owners; }
        }

        /// <summary>
        /// Gets a value indicating whether the this security permissions set contains any identities. 
        /// </summary>
        public bool HasSecurityData
        {
            get
            {
                return _owners.Count > 0;
            }
        }

        /// <summary>
        /// Gets an enumeration of security identities that match the specified permission scope. 
        /// </summary>
        /// <param name="permissions">Permission scope</param>
        /// <param name="identityScope">Identity scope predicate.</param>
        /// <returns>Enumeration of security identities.</returns>
        public IEnumerable<SecurityIdentity> GetScopeIdentities(PermissionScope permissions, IdentityScope? identityScope = null, bool merge = true)
        {
            // Readers = Readers | Writers | Owners 
            // Writers = Writers | Owners
            // Owners = Owners 

            IEnumerable<SecurityIdentity> enumerable = merge ? GetPermissionScopeSet(permissions) : GetSpecificPermissionsScopeHashSet(permissions);

            if (identityScope != null)
            {
                enumerable = enumerable.Where(i => i.Scope == identityScope.Value);
            }

            return enumerable;
        }

        public SecurityIdentity FindIdentityByName(string identity, PermissionScope scope)
        {
            return this.GetPermissionScopeSet(scope).FirstOrDefault(i => String.Equals(identity, i.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a value indicating whether the specified identity is a member of the specified permission scope. 
        /// </summary>
        /// <param name="type">Identity scope.</param>
        /// <param name="identity">Identity.</param>
        /// <param name="scope">Permissions scope.</param>
        /// <returns>True is the specified identity is within the specified scope, otherwise false.</returns>
        public bool IsIdentityInPermissionScope(IdentityScope type, string identity, PermissionScope scope)
        {
            var si = SecurityIdentity.Create(type, identity);

            // TODO: Consider making a virtual hash set and calling contains (it'll be faster). 
            return this.GetScopeIdentities(scope, type).Any(i => i.Equals(si));
        }

        /// <summary>
        /// Grant the specified identity for the specified permission scope. 
        /// </summary>
        /// <param name="type">Type of Identity (Group, User)</param>
        /// <param name="identity">Identity to grant permissions to (DOMAIN\user).</param>
        /// <param name="scope">Scope of permissions to grant (Owner, Writer, Reader)</param>
        public void Grant(IdentityScope type, string identity, PermissionScope scope)
        {
            Grant(SecurityIdentity.Create(type, identity), scope);
        }

        /// <summary>
        /// Grant the specified identity for the specified permission scope. 
        /// </summary>
        /// <param name="identity">Identity to grant permissions to.</param>
        /// <param name="scope">Scope of permissions to grant (Owner, Writer, Reader)</param>
        public void Grant(SecurityIdentity identity, PermissionScope scope)
        {
            if (_owners.Count == 0 && scope != PermissionScope.Owner)
            {
                throw new ArribaException(String.Format("Unable to grant permission scope {0} as there are no identities with owner scope", scope));
            }

            var target = GetSpecificPermissionsScopeHashSet(scope);
            target.Add(identity);
        }


        /// <summary>
        /// Revoke the specified identity for the specified permission scope. 
        /// </summary>
        /// <param name="type">Type of Identity (Group, User)</param>
        /// <param name="identity">Identity to revoke permissions from (DOMAIN\user).</param>
        /// <param name="scope">Scope of permissions to revoke (Owner, Writer, Reader)</param>
        public void Revoke(IdentityScope type, string identity, PermissionScope scope)
        {
            Revoke(SecurityIdentity.Create(type, identity), scope);
        }

        /// <summary>
        /// Revoke the specified identity for the specified permission scope. 
        /// </summary>
        /// <param name="identity">Identity to revoke permissions from.</param>
        /// <param name="scope">Scope of permissions to revoke (Owner, Writer, Reader)</param>
        public void Revoke(SecurityIdentity identity, PermissionScope scope)
        {
            var target = GetSpecificPermissionsScopeHashSet(scope);
            target.Remove(identity);
        }

        /// <summary>
        /// Gets the permission scope hashset for the specified scope.
        /// </summary>
        private HashSet<SecurityIdentity> GetSpecificPermissionsScopeHashSet(PermissionScope scope)
        {
            switch (scope)
            {
                case PermissionScope.Owner:
                    return _owners;
                case PermissionScope.Reader:
                    return _readers;
                case PermissionScope.Writer:
                    return _writers;
                default:
                    throw new ArribaException(String.Format("Unknown permission scope {0}", scope));
            }
        }

        private IEnumerable<SecurityIdentity> GetPermissionScopeSet(PermissionScope permissions)
        {
            switch (permissions)
            {
                case PermissionScope.Reader:
                    return _readers.Concat(_writers).Concat(_owners);
                case PermissionScope.Writer:
                    return _writers.Concat(_owners);
                case PermissionScope.Owner:
                    return _owners;
                default:
                    throw new ArribaException(String.Format("Unknown permission scope {0}", permissions));
            }
        }

        #region Serialization
        public void ReadBinary(ISerializationContext context)
        {
            _readers = ReadHashSet(context.Reader);
            _writers = ReadHashSet(context.Reader);
            _owners = ReadHashSet(context.Reader);
        }

        public void WriteBinary(ISerializationContext context)
        {
            WriteHashSet(context.Writer, _readers);
            WriteHashSet(context.Writer, _writers);
            WriteHashSet(context.Writer, _owners);
        }

        private static void WriteHashSet(BinaryWriter binaryWriter, HashSet<SecurityIdentity> hashSet)
        {
            binaryWriter.Write(hashSet.Count);

            foreach (var item in hashSet)
            {
                binaryWriter.Write((byte)item.Scope);
                binaryWriter.Write(item.Name);
            }
        }

        private static HashSet<SecurityIdentity> ReadHashSet(BinaryReader binaryReader)
        {
            int count = binaryReader.ReadInt32();
            HashSet<SecurityIdentity> value = new HashSet<SecurityIdentity>();

            for (int i = 0; i < count; i++)
            {
                value.Add(SecurityIdentity.Create((IdentityScope)binaryReader.ReadByte(), binaryReader.ReadString()));
            }

            return value;
        }
        #endregion
    }
}
