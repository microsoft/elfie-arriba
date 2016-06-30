// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Arriba.Model.Security;
using Arriba.Serialization;

namespace Arriba.Model
{
    /// <summary>
    ///  SecureDatabase adds [optional] SecurityPermissions next to Tables for
    ///  remote Arriba scenarios.
    /// </summary>
    public class SecureDatabase : Database
    {
        private Dictionary<string, SecurityPermissions> _securityByTable;

        public SecureDatabase() : base()
        {
            _securityByTable = new Dictionary<string, SecurityPermissions>(StringComparer.OrdinalIgnoreCase);
        }

        public SecurityPermissions Security(string tableName)
        {
            SecurityPermissions security;

            if (_securityByTable.TryGetValue(tableName, out security))
            {
                // Return cached Security, if found
                return security;
            }
            else
            {
                // Construct a new [empty] SecurityPermissions
                security = new SecurityPermissions();

                // Load previously serialized permissions if found
                string securityPath = SecurityCachePath(tableName);
                if (File.Exists(securityPath))
                {
                    security.Read(securityPath);
                }

                // Cache the created|loaded security
                _securityByTable[tableName] = security;

                return security;
            }
        }

        public void SetSecurity(string tableName, SecurityPermissions security)
        {
            _securityByTable[tableName] = security;
        }

        public void SaveSecurity(string tableName)
        {
            string securityPath = SecurityCachePath(tableName);
            _securityByTable[tableName].Write(securityPath);
        }

        private string SecurityCachePath(string tableName)
        {
            string tablePath = Table.TableCachePath(tableName);
            return Path.Combine(tablePath, "Metadata", "security.bin");
        }
    }
}
