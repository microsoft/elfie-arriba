// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Serialization;

namespace Arriba.Model.Security
{
    /// <summary>
    /// Represents access permissions to a resource. 
    /// </summary>
    public class SecurityPermissions : IBinarySerializable
    {
        public SecurityPermissions()
        {
            this.Readers = new HashSet<SecurityIdentity>();
            this.Writers = new HashSet<SecurityIdentity>();
            this.Owners = new HashSet<SecurityIdentity>();

            this.RestrictedColumns = new List<Tuple<SecurityIdentity, List<string>>>();
            this.RowRestrictedUsers = new List<Tuple<SecurityIdentity, string>>();
        }

        /// <summary>
        ///  Set of Identities which can read from most table columns
        /// </summary>
        public HashSet<SecurityIdentity> Readers { get; set; }

        /// <summary>
        ///  Set of identities which can read and write to most table columns
        /// </summary>
        public HashSet<SecurityIdentity> Writers { get; set; }

        /// <summary>
        ///  Set of identities which can read, write, alter schema, and change security on the table.
        /// </summary>
        public HashSet<SecurityIdentity> Owners { get; set; }

        /// <summary>
        ///  List of identities with access to restricted columns and which columns they have access to.
        ///  If a user is not in the given group, all of the listed columns are excluded from queries and results.
        /// </summary>
        public List<Tuple<SecurityIdentity, List<string>>> RestrictedColumns { get; set; }

        /// <summary>
        ///  List of identities with limited row access, implemented via an additional query clause
        ///  added to all queries.
        /// </summary>
        private List<Tuple<SecurityIdentity, string>> RowRestrictedUsers { get; set; }

        /// <summary>
        /// Gets a value indicating whether the this security permissions set contains any identities. 
        /// </summary>
        public bool HasSecurityData
        {
            get
            {
                return Owners.Count > 0;
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
            if (Owners.Count == 0 && scope != PermissionScope.Owner)
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
        ///  Restrict access to a column set to a specific identity.
        /// </summary>
        /// <param name="identity">Identity which is allowed column access</param>
        /// <param name="columnList">Set of column names only readable by this identity</param>
        public void SecureColumns(SecurityIdentity identity, IEnumerable<string> columnList)
        {
            RestrictedColumns.Add(new Tuple<SecurityIdentity, List<string>>(identity, new List<string>(columnList)));
        }

        /// <summary>
        ///  Remove column restrictions for the given identity.
        /// </summary>
        /// <param name="identity">Identity with restricted access to un-restrict</param>
        public void UnsecureColumns(SecurityIdentity identity)
        {
            for (int i = 0; i < RestrictedColumns.Count; ++i)
            {
                if(RestrictedColumns[i].Item1.Equals(identity))
                {
                    RestrictedColumns.RemoveAt(i);
                    --i;
                }
            }
        }

        /// <summary>
        ///  Restrict access for an identity to a set of rows.
        /// </summary>
        /// <param name="identity">Identity to restrict row access for</param>
        /// <param name="filterQuery">Query to AND with all queries to restrict rows</param>
        public void SecureRows(SecurityIdentity identity, string filterQuery)
        {
            RowRestrictedUsers.Add(new Tuple<SecurityIdentity, string>(identity, filterQuery));
        }

        /// <summary>
        ///  Remove row restriction for identity.
        /// </summary>
        /// <param name="identity"></param>
        public void UnsecureRows(SecurityIdentity identity)
        {
            for(int i = 0; i < RowRestrictedUsers.Count; ++i)
            {
                if (RowRestrictedUsers[i].Item1.Equals(identity))
                {
                    RowRestrictedUsers.RemoveAt(i);
                    --i;
                }
            }
        }

        /// <summary>
        /// Gets the permission scope hashset for the specified scope.
        /// </summary>
        private HashSet<SecurityIdentity> GetSpecificPermissionsScopeHashSet(PermissionScope scope)
        {
            switch (scope)
            {
                case PermissionScope.Owner:
                    return Owners;
                case PermissionScope.Reader:
                    return Readers;
                case PermissionScope.Writer:
                    return Writers;
                default:
                    throw new ArribaException(String.Format("Unknown permission scope {0}", scope));
            }
        }

        private IEnumerable<SecurityIdentity> GetPermissionScopeSet(PermissionScope permissions)
        {
            switch (permissions)
            {
                case PermissionScope.Reader:
                    return Readers.Concat(Writers).Concat(Owners);
                case PermissionScope.Writer:
                    return Writers.Concat(Owners);
                case PermissionScope.Owner:
                    return Owners;
                default:
                    throw new ArribaException(String.Format("Unknown permission scope {0}", permissions));
            }
        }

        #region Serialization
        public void ReadBinary(ISerializationContext context)
        {
            Readers = ReadHashSet(context);
            Writers = ReadHashSet(context);
            Owners = ReadHashSet(context);

            // Read column security rules
            RestrictedColumns = new List<Tuple<SecurityIdentity, List<string>>>();
            int columnRuleCount = context.Reader.ReadInt32();
            for(int i = 0; i < columnRuleCount; ++i)
            {
                SecurityIdentity identity = new SecurityIdentity();
                identity.ReadBinary(context);

                List<string> columnsSecured = new List<string>();

                int columnsForGroupCount = context.Reader.ReadInt32();
                for(int j = 0; j < columnsForGroupCount; ++j)
                {
                    columnsSecured.Add(context.Reader.ReadString());
                }

                RestrictedColumns.Add(new Tuple<SecurityIdentity, List<string>>(identity, columnsSecured));
            }

            // Read row security rules
            RowRestrictedUsers = new List<Tuple<SecurityIdentity, string>>();
            int rowRuleCount = context.Reader.ReadInt32();
            for (int i = 0; i < rowRuleCount; ++i)
            {
                SecurityIdentity identity = new SecurityIdentity();
                identity.ReadBinary(context);

                string filterForIdentity = context.Reader.ReadString();
                RowRestrictedUsers.Add(new Tuple<SecurityIdentity, string>(identity, filterForIdentity));
            }
        }

        public void WriteBinary(ISerializationContext context)
        {
            WriteHashSet(context, Readers);
            WriteHashSet(context, Writers);
            WriteHashSet(context, Owners);

            // Write column security rules
            context.Writer.Write(RestrictedColumns.Count);
            foreach(Tuple<SecurityIdentity, List<string>> columnRule in RestrictedColumns)
            {
                columnRule.Item1.WriteBinary(context);
                context.Writer.Write(columnRule.Item2.Count);
                foreach (string columnName in columnRule.Item2)
                {
                    context.Writer.Write(columnName);
                }
            }

            // Write row security rules
            context.Writer.Write(RowRestrictedUsers.Count);
            foreach(Tuple<SecurityIdentity, string> rowRule in RowRestrictedUsers)
            {
                rowRule.Item1.WriteBinary(context);
                context.Writer.Write(rowRule.Item2);
            }
        }

        private static void WriteHashSet(ISerializationContext context, HashSet<SecurityIdentity> hashSet)
        {
            context.Writer.Write(hashSet.Count);

            foreach (var item in hashSet)
            {
                item.WriteBinary(context);
            }
        }

        private static HashSet<SecurityIdentity> ReadHashSet(ISerializationContext context)
        {
            int count = context.Reader.ReadInt32();
            HashSet<SecurityIdentity> value = new HashSet<SecurityIdentity>();

            for (int i = 0; i < count; i++)
            {
                SecurityIdentity identity = new SecurityIdentity();
                identity.ReadBinary(context);
                value.Add(identity);
            }

            return value;
        }
        #endregion
    }
}
