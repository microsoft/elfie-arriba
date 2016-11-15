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
        /// <summary>
        ///  Set of Identities which can read from most table columns
        /// </summary>
        private HashSet<SecurityIdentity> _readers = new HashSet<SecurityIdentity>();

        /// <summary>
        ///  Set of identities which can read and write to most table columns
        /// </summary>
        private HashSet<SecurityIdentity> _writers = new HashSet<SecurityIdentity>();

        /// <summary>
        ///  Set of identities which can read, write, alter schema, and change security on the table.
        /// </summary>
        private HashSet<SecurityIdentity> _owners = new HashSet<SecurityIdentity>();

        /// <summary>
        ///  List of identities with access to restricted columns and which columns they have access to.
        ///  If a user is not in the given group, all of the listed columns are excluded from queries and results.
        /// </summary>
        private List<Tuple<SecurityIdentity, IList<string>>> _columnSecurity = new List<Tuple<SecurityIdentity, IList<string>>>();

        /// <summary>
        ///  List of identities with access to restricted rows, implemented via an additional query clause
        ///  added to all queries.
        /// </summary>
        private List<Tuple<SecurityIdentity, string>> _rowSecurity = new List<Tuple<SecurityIdentity, string>>();

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

        public IEnumerable<Tuple<SecurityIdentity, IList<string>>> ColumnSecurity
        {
            get { return _columnSecurity; }
        }

        public IEnumerable<Tuple<SecurityIdentity, string>> RowSecurity
        {
            get { return _rowSecurity; }
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
        ///  Restrict access to a column set to a specific identity.
        /// </summary>
        /// <param name="identity">Identity which is allowed column access</param>
        /// <param name="columnList">Set of column names only readable by this identity</param>
        public void SecureColumns(SecurityIdentity identity, IEnumerable<string> columnList)
        {
            _columnSecurity.Add(new Tuple<SecurityIdentity, IList<string>>(identity, new List<string>(columnList)));
        }

        /// <summary>
        ///  Remove column restrictions for the given identity.
        /// </summary>
        /// <param name="identity">Identity with restricted access to un-restrict</param>
        public void UnsecureColumns(SecurityIdentity identity)
        {
            for (int i = 0; i < _columnSecurity.Count; ++i)
            {
                if(_columnSecurity[i].Item1.Equals(identity))
                {
                    _columnSecurity.RemoveAt(i);
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
            _rowSecurity.Add(new Tuple<SecurityIdentity, string>(identity, filterQuery));
        }

        /// <summary>
        ///  Remove row restriction for identity.
        /// </summary>
        /// <param name="identity"></param>
        public void UnsecureRows(SecurityIdentity identity)
        {
            for(int i = 0; i < _rowSecurity.Count; ++i)
            {
                if (_rowSecurity[i].Item1.Equals(identity))
                {
                    _rowSecurity.RemoveAt(i);
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
            _readers = ReadHashSet(context);
            _writers = ReadHashSet(context);
            _owners = ReadHashSet(context);

            // Read column security rules
            _columnSecurity = new List<Tuple<SecurityIdentity, IList<string>>>();
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

                _columnSecurity.Add(new Tuple<SecurityIdentity, IList<string>>(identity, columnsSecured));
            }

            // Read row security rules
            _rowSecurity = new List<Tuple<SecurityIdentity, string>>();
            int rowRuleCount = context.Reader.ReadInt32();
            for (int i = 0; i < rowRuleCount; ++i)
            {
                SecurityIdentity identity = new SecurityIdentity();
                identity.ReadBinary(context);

                string filterForIdentity = context.Reader.ReadString();
                _rowSecurity.Add(new Tuple<SecurityIdentity, string>(identity, filterForIdentity));
            }
        }

        public void WriteBinary(ISerializationContext context)
        {
            WriteHashSet(context, _readers);
            WriteHashSet(context, _writers);
            WriteHashSet(context, _owners);

            // Write column security rules
            context.Writer.Write(_columnSecurity.Count);
            foreach(Tuple<SecurityIdentity, IList<string>> columnRule in _columnSecurity)
            {
                columnRule.Item1.WriteBinary(context);
                context.Writer.Write(columnRule.Item2.Count);
                foreach (string columnName in columnRule.Item2)
                {
                    context.Writer.Write(columnName);
                }
            }

            // Write row security rules
            context.Writer.Write(_rowSecurity.Count);
            foreach(Tuple<SecurityIdentity, string> rowRule in _rowSecurity)
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
