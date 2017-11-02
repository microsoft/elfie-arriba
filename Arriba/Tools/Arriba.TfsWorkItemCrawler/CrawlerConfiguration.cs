// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Security;

namespace Arriba.TfsWorkItemCrawler
{
    public class CrawlerConfiguration
    {
        internal const string IdentityFormatExceptionFormatString = @"Identity must be of the format User:DOMAIN\name or Group:DOMAIN\name. Value passed, '{0}', doesn't fit these rules.";

        /// <summary>
        ///  The name of the configuration itself [Sample]
        /// </summary>
        public string ConfigurationName { get; set; }

        /// <summary>
        ///  The URL of the Arriba Service to crawl to.
        ///  [Ex: http://localhost:42784]
        /// </summary>
        public string ArribaServiceUrl { get; set; }

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
        ///  The set of columns to include. If set, only these columns will be crawled.
        /// </summary>
        public List<string> ColumnsToInclude { get; set; }

        /// <summary>
        ///  The set of columns to exclude. All columns from the source
        ///  are added by default.
        /// </summary>
        public List<string> ColumnsToExclude { get; set; }

        /// <summary>
        ///  Name of IItemProvider to use for crawl. [TfsItemProvider]
        /// </summary>
        public string ItemProvider { get; set; }

        /// <summary>
        ///  Name of IItemConsumer to write to on crawl. [ArribaClient, ArribaDirect, CsvWriter]
        /// </summary>
        public string ItemConsumer { get; set; }

        /// <summary>
        ///  Name/Url of source database. [https://projectName.visualstudio.com]
        /// </summary>
        public string ItemDatabaseName { get; set; }

        /// <summary>
        ///  WIQL Query for items to index. If not provided, all items are retrieved.
        ///  Query must contain [AND ChangedDate &gt;= @Start AND ChangedDate &lt;= @End]
        ///  so that the crawler can scope items to retrieve on each pass.
        /// </summary>
        public string ItemQuery { get; set; }

        /// <summary>
        ///  Authentication Mode to use with provider (provider specific).
        ///  For TfsItemProvider: integrated (default), aad, alternate (deprecated)
        /// </summary>
        public string AuthenticationMode { get; set; }

        /// <summary>
        ///  The User Name to use for provider authentication (if the mode requires one)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///  List of columns to be renamed from the source, if any.
        /// </summary>
        public Dictionary<string, string> ColumnMappings { get; set; }

        public CrawlerConfiguration()
        {
            this.Owners = new List<string>();
            this.Writers = new List<string>();
            this.Readers = new List<string>();
            this.ColumnsToInclude = new List<string>();
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
