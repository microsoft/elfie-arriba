using Arriba.Model;
using Arriba.Model.Column;
using System;
using System.Collections.Generic;
using Arriba.Model.Security;
using System.Linq;

namespace Arriba.TfsWorkItemCrawler
{
    public class CrawlerConfiguration
    {
        internal const string IdentityFormatExceptionFormatString = @"Identity must be of the format User:DOMAIN\name or Group:DOMAIN\name. Value passed, '{0}', doesn't fit these rules.";

        /// <summary>
        ///  Name of Arriba Table to create/manage
        /// </summary>
        public string ArribaTable { get; set; }

        /// <summary>
        ///  Maximum number of items in Table. The number of partitions to contain
        ///  this number of items will be created.
        /// </summary>
        public long ItemCountLimit { get; set; }

        /// <summary>
        ///  The set of groups or users with administrative access to the table.
        ///  [Set Permissions, Drop Table, Write, Read]
        /// </summary>
        public List<string> Owners { get; set; }

        /// <summary>
        ///  The set of groups or users with write access to the table.
        ///  [Write, Read]
        /// </summary>
        public List<string> Writers { get; set; }

        /// <summary>
        ///  The set of groups or users with read access to the table.
        /// </summary>
        public List<string> Readers { get; set; }

        /// <summary>
        ///  The set of columns to exclude. All columns from the source
        ///  are added by default.
        /// </summary>
        public List<string> ColumnsToExclude { get; set; }

        public string ItemProvider { get; set; }
        public string ItemDatabaseName { get; set; }
        public string ItemQuery { get; set; }

        public string TfsOnlineUserName { get; set; }
        public string TfsOnlineEncryptedPasswordFilePath { get; set; }
        public bool UseAADForTFSAuth { get; set; }

        public Dictionary<string, string> ColumnMappings { get; set; }

        public CrawlerConfiguration()
        {
            this.Owners = new List<string>();
            this.Writers = new List<string>();
            this.Readers = new List<string>();
            this.ColumnsToExclude = new List<string>();
            this.ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public SecurityPermissions LoadPermissions()
        {
            SecurityPermissions result = new SecurityPermissions();

            foreach (SecurityIdentity identity in ParseIdentities(this.Owners))
            {
                result.Grant(identity, PermissionScope.Owner);
            }

            foreach (SecurityIdentity identity in ParseIdentities(this.Writers))
            {
                result.Grant(identity, PermissionScope.Writer);
            }

            foreach (SecurityIdentity identity in ParseIdentities(this.Readers))
            {
                result.Grant(identity, PermissionScope.Reader);
            }

            return result;
        }

        private static IEnumerable<SecurityIdentity> ParseIdentities(IEnumerable<string> identities)
        {
            List<SecurityIdentity> result = new List<SecurityIdentity>();

            if (identities != null)
            {
                foreach (string identityPart in identities)
                {
                    result.Add(ParseIdentity(identityPart.Trim()));
                }
            }

            return result;
        }

        private static SecurityIdentity ParseIdentity(string identity)
        {
            string[] parts = identity.Split(new char[] { ':' });
            if (parts.Length != 2) throw new ArgumentException(String.Format(IdentityFormatExceptionFormatString, identity));

            switch (parts[0].ToLowerInvariant())
            {
                case "u":
                case "user":
                    return SecurityIdentity.Create(IdentityScope.User, parts[1]);
                case "g":
                case "group":
                    return SecurityIdentity.Create(IdentityScope.Group, parts[1]);
                default:
                    throw new ArgumentException(String.Format(IdentityFormatExceptionFormatString, identity));
            }
        }
    }
}
