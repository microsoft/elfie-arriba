// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Arriba.Model.Column;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
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

        public SecurityPermissions DatabasePermissions()
        {
            return Security("");
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
                lock (_tableLock)
                {
                    _securityByTable[tableName] = security;
                }

                return security;
            }
        }

        public override T Query<T>(IQuery<T> query)
        {
            throw new ArribaException("Use Query overload which takes isCurrentUserIn on SecureDatabase.");
        }

        public T Query<T>(IQuery<T> query, Func<SecurityIdentity, bool> isCurrentUserIn)
        {
            ExecutionDetails preExecuteDetails = new ExecutionDetails();
            ApplyTableSecurity(query, isCurrentUserIn, preExecuteDetails);

            T result = base.Query<T>(query);
            if (result is IBaseResult)
            {
                ((IBaseResult)result).Details.Merge(preExecuteDetails);
            }

            return result;
        }

        public IList<string> GetRestrictedColumns(string tableName, Func<SecurityIdentity, bool> isCurrentUserIn)
        {
            List<string> restrictedColumns = null;
            SecurityPermissions security = this.Security(tableName);

            foreach (var columnRestriction in security.RestrictedColumns)
            {
                if (!isCurrentUserIn(columnRestriction.Key))
                {
                    if (restrictedColumns == null) restrictedColumns = new List<string>();
                    restrictedColumns.AddRange(columnRestriction.Value);
                }
            }
            return restrictedColumns;
        }

        protected void ApplyTableSecurity<T>(IQuery<T> query, Func<SecurityIdentity, bool> isCurrentUserIn, ExecutionDetails details)
        {
            SecurityPermissions security = this.Security(query.TableName);

            // If table has row restrictions and one matches, restrict rows and allow
            // NOTE: If restricted rows are returned, columns are NOT restricted.
            foreach (var rowRestriction in security.RowRestrictedUsers)
            {
                if (isCurrentUserIn(rowRestriction.Key))
                {
                    query.Where = new AndExpression(QueryParser.Parse(rowRestriction.Value), query.Where);
                    return;
                }
            }

            // If table has column restrictions, build a list of excluded columns
            IList<string> restrictedColumns = GetRestrictedColumns(query.TableName, isCurrentUserIn);

            // If no columns were restricted, return query as-is
            if (restrictedColumns == null) return;

            // Exclude disallowed columns from where clauses
            // If a disallowed column is requested specifically, block the query and return an error
            ColumnSecurityCorrector c = new ColumnSecurityCorrector(restrictedColumns);
            try
            {
                query.Correct(c);
            }
            catch (ArribaColumnAccessDeniedException e)
            {
                query.Where = new EmptyExpression();
                details.AddDeniedColumn(e.Message);
                details.AddError(ExecutionDetails.DisallowedColumnQuery, e.Message);
            }

            // If columns are excluded, remove those from the select list
            IQuery<T> primaryQuery = query;
            if (query is JoinQuery<T>) primaryQuery = ((JoinQuery<T>)query).PrimaryQuery;

            if (primaryQuery.GetType().Equals(typeof(SelectQuery)))
            {
                SelectQuery sq = (SelectQuery)primaryQuery;
                List<string> filteredColumns = null;

                if (sq.Columns.Count == 1 && sq.Columns[0] == "*")
                {
                    filteredColumns = new List<string>();
                    foreach (ColumnDetails column in this[sq.TableName].ColumnDetails)
                    {
                        if (restrictedColumns.Contains(column.Name))
                        {
                            details.AddDeniedColumn(column.Name);
                        }
                        else
                        {
                            filteredColumns.Add(column.Name);
                        }
                    }
                }
                else
                {
                    foreach (string columnName in sq.Columns)
                    {
                        if (restrictedColumns.Contains(columnName))
                        {
                            if (filteredColumns == null) filteredColumns = new List<string>(sq.Columns);
                            filteredColumns.Remove(columnName);

                            details.AddDeniedColumn(columnName);
                        }
                    }
                }

                if (filteredColumns != null) sq.Columns = filteredColumns;
            }
            else if (primaryQuery.GetType().Equals(typeof(AggregationQuery)))
            {
                AggregationQuery aq = (AggregationQuery)primaryQuery;
                if (aq.AggregationColumns != null)
                {
                    foreach (string columnName in aq.AggregationColumns)
                    {
                        if (restrictedColumns.Contains(columnName))
                        {
                            details.AddDeniedColumn(columnName);
                            details.AddError(ExecutionDetails.DisallowedColumnQuery, columnName);
                            aq.Where = new EmptyExpression();
                        }
                    }
                }
            }
            else if (primaryQuery.GetType().Equals(typeof(DistinctQuery)))
            {
                DistinctQuery dq = (DistinctQuery)primaryQuery;
                if (restrictedColumns.Contains(dq.Column))
                {
                    details.AddDeniedColumn(dq.Column);
                    details.AddError(ExecutionDetails.DisallowedColumnQuery, dq.Column);
                    dq.Where = new EmptyExpression();
                }
            }
            else
            {
                // IQuery is extensible; there's no way to ensure that user-implemented
                // queries respect security rules.
                details.AddError(ExecutionDetails.DisallowedQuery, primaryQuery.GetType().Name);
                primaryQuery.Where = new EmptyExpression();
            }
        }

        public void SetSecurity(string tableName, SecurityPermissions security)
        {
            lock (_tableLock)
            {
                _securityByTable[tableName] = security;
            }
        }

        public void SaveSecurity(string tableName)
        {
            string securityPath = SecurityCachePath(tableName);
            _securityByTable[tableName].Write(securityPath);
        }

        private string SecurityCachePath(string tableName)
        {
            // Database Level Security (for table creation)
            if(tableName == "") return BinarySerializable.FullPath("security.bin");

            // Table-specific security
            string tablePath = Table.TableCachePath(tableName);
            return Path.Combine(tablePath, "Metadata", "security.bin");
        }

        public override void ReloadTable(string tableName)
        {
            lock (_tableLock)
            {
                // Reload the table
                base.ReloadTable(tableName);

                // Reload the security
                _securityByTable.Remove(tableName);
                Security(tableName);
            }
        }

        public override void UnloadTable(string tableName)
        {
            // Unload the table and security data
            lock (_tableLock)
            {
                base.UnloadTable(tableName);
                _securityByTable.Remove(tableName);
            }
        }

        public override void UnloadAll()
        {
            // Unload all tables and security data
            lock (_tableLock)
            {
                base.UnloadAll();
                _securityByTable.Clear();
            }
        }
    }
}
