// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Types;

namespace Arriba.Server.Application
{
    [Export(typeof(IRoutedApplication))]
    internal class ArribaManagement : ArribaApplication
    {
        [ImportingConstructor]
        public ArribaManagement(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
            // GET - return tables in Database
            this.Get("", this.GetTables);

            this.Get("/allBasics", this.GetAllBasics);

            this.Get("/unloadAll", this.ValidateCreateAccess, this.UnloadAll);

            // GET /table/foo - Get table information 
            this.Get("/table/:tableName", this.ValidateReadAccess, this.GetTableInformation);

            // POST /table with create table payload (Must be Writer/Owner in security directly in DiskCache folder, or identity running service)
            this.PostAsync("/table", this.ValidateCreateAccessAsync, this.ValidateBodyAsync, this.CreateNew);

            // POST /table/foo/addcolumns
            this.PostAsync("/table/:tableName/addcolumns", this.ValidateWriteAccessAsync, this.AddColumns);

            // GET /table/foo/save -- TODO: This is not ideal, think of a better pattern 
            this.Get("/table/:tableName/save", this.ValidateWriteAccess, this.Save);

            // Unload/Reload
            this.Get("/table/:tableName/unload", this.ValidateWriteAccess, this.UnloadTable);
            this.Get("/table/:tableName/reload", this.ValidateWriteAccess, this.Reload);

            // DELETE /table/foo 
            this.Delete("/table/:tableName", this.ValidateOwnerAccess, this.Drop);
            this.Get("/table/:tableName/delete", this.ValidateOwnerAccess, this.Drop);

            // POST /table/foo?action=delete
            this.Get(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.ValidateWriteAccess, this.DeleteRows);
            this.Post(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.ValidateWriteAccess, this.DeleteRows);

            // POST /table/foo/permissions/user - add permissions 
            this.PostAsync("/table/:tableName/permissions/:scope", this.ValidateOwnerAccessAsync, this.ValidateBodyAsync, this.Grant);

            // DELETE /table/foo/permissions/user - remove permissions from table 
            this.DeleteAsync("/table/:tableName/permissions/:scope", this.ValidateOwnerAccessAsync, this.ValidateBodyAsync, this.Revoke);

            // NOTE: _SPECIAL_ permission for localhost users, will override current auth to always be valid.
            // this enables tables recovery from local machine for matching user as the process. 
            // GET /table/foo/permissions  
            this.Get("/table/:tableName/permissions",
                    (c, r) => this.ValidateTableAccess(c, r, PermissionScope.Reader, overrideLocalHostSameUser: true),
                    this.GetTablePermissions);

            // POST /table/foo/permissions  
            this.PostAsync("/table/:tableName/permissions",
                     async (c, r) => await this.ValidateTableAccessAsync(c, r, PermissionScope.Owner, overrideLocalHostSameUser: true),
                     this.SetTablePermissions);
        }

        private IResponse GetTables(IRequestContext ctx, Route route)
        {
            return ArribaResponse.Ok(this.Database.TableNames);
        }

        private IResponse GetAllBasics(IRequestContext ctx, Route route)
        {
            bool hasTables = false;

            Dictionary<string, TableInformation> allBasics = new Dictionary<string, TableInformation>();
            foreach (string tableName in this.Database.TableNames)
            {
                hasTables = true;

                if (HasTableAccess(tableName, ctx.Request.User, PermissionScope.Reader))
                {
                    allBasics[tableName] = GetTableBasics(tableName, ctx);
                }
            }

            // If you didn't have access to any tables, return a distinct result to show Access Denied in the browser
            // but not a 401, because that is eaten by CORS.
            if (allBasics.Count == 0 && hasTables)
            {
                return ArribaResponse.Ok(null);
            }

            return ArribaResponse.Ok(allBasics);
        }

        private IResponse GetTableInformation(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound();
            }

            TableInformation ti = GetTableBasics(tableName, ctx);
            return ArribaResponse.Ok(ti);
        }

        private TableInformation GetTableBasics(string tableName, IRequestContext ctx)
        {
            var table = this.Database[tableName];

            TableInformation ti = new TableInformation();
            ti.Name = tableName;
            ti.PartitionCount = table.PartitionCount;
            ti.RowCount = table.Count;
            ti.LastWriteTimeUtc = table.LastWriteTimeUtc;
            ti.CanWrite = HasTableAccess(tableName, ctx.Request.User, PermissionScope.Writer);
            ti.CanAdminister = HasTableAccess(tableName, ctx.Request.User, PermissionScope.Owner);

            IList<string> restrictedColumns = this.Database.GetRestrictedColumns(tableName, (si) => this.IsInIdentity(ctx.Request.User, si));
            if (restrictedColumns == null)
            {
                ti.Columns = table.ColumnDetails;
            }
            else
            {
                List<ColumnDetails> allowedColumns = new List<ColumnDetails>();
                foreach (ColumnDetails column in table.ColumnDetails)
                {
                    if (!restrictedColumns.Contains(column.Name)) allowedColumns.Add(column);
                }
                ti.Columns = allowedColumns;
            }

            return ti;
        }

        private IResponse UnloadTable(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            this.Database.UnloadTable(tableName);
            return ArribaResponse.Ok($"Table unloaded");
        }

        private IResponse UnloadAll(IRequestContext ctx, Route route)
        {
            this.Database.UnloadAll();
            return ArribaResponse.Ok("All Tables unloaded");
        }

        private IResponse Drop(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound();
            }

            using (ctx.Monitor(MonitorEventLevel.Information, "Drop", type: "Table", identity: tableName))
            {
                this.Database.DropTable(tableName);
                return ArribaResponse.Ok("Table deleted");
            }
        }

        private IResponse GetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to return security for.");
            }

            var security = this.Database.Security(tableName);
            return ArribaResponse.Ok(security);
        }


        private IResponse DeleteRows(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            IExpression query = SelectQuery.ParseWhere(ctx.Request.ResourceParameters["q"]);

            // Run server correctors
            query = this.CurrentCorrectors(ctx).Correct(query);

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound();
            }

            Table table = this.Database[tableName];
            DeleteResult result = table.Delete(query);

            return ArribaResponse.Ok(result.Count);
        }

        private async Task<IResponse> SetTablePermissions(IRequestContext request, Route route)
        {
            SecurityPermissions security = await request.Request.ReadBodyAsync<SecurityPermissions>();
            string tableName = GetAndValidateTableName(route);

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table doesn't exist to update security for.");
            }

            // Reset table permissions and save them
            this.Database.SetSecurity(tableName, security);
            this.Database.SaveSecurity(tableName);

            return ArribaResponse.Ok("Security Updated");
        }

        private async Task<IResponse> CreateNew(IRequestContext request, Route routeData)
        {
            CreateTableRequest createTable = await request.Request.ReadBodyAsync<CreateTableRequest>();

            if (createTable == null)
            {
                return ArribaResponse.BadRequest("Invalid body");
            }

            // Does the table already exist? 
            if (this.Database.TableExists(createTable.TableName))
            {
                return ArribaResponse.BadRequest("Table already exists");
            }

            using (request.Monitor(MonitorEventLevel.Information, "Create", type: "Table", identity: createTable.TableName, detail: createTable))
            {
                var table = this.Database.AddTable(createTable.TableName, createTable.ItemCountLimit);

                // Add columns from request
                table.AddColumns(createTable.Columns);

                // Include permissions from request
                if (createTable.Permissions != null)
                {
                    // Ensure the creating user is always an owner
                    createTable.Permissions.Grant(IdentityScope.User, request.Request.User.Identity.Name, PermissionScope.Owner);

                    this.Database.SetSecurity(createTable.TableName, createTable.Permissions);
                }

                // Save, so that table existence, column definitions, and permissions are saved
                table.Save();
                this.Database.SaveSecurity(createTable.TableName);
            }

            return ArribaResponse.Ok(null);
        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        private async Task<IResponse> AddColumns(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);

            using (request.Monitor(MonitorEventLevel.Information, "AddColumn", type: "Table", identity: tableName))
            {
                if (!Database.TableExists(tableName))
                {
                    return ArribaResponse.NotFound("Table not found to Add Columns to.");
                }

                Table table = this.Database[tableName];

                List<ColumnDetails> columns = await request.Request.ReadBodyAsync<List<ColumnDetails>>();
                table.AddColumns(columns);

                return ArribaResponse.Ok("Added");
            }
        }

        /// <summary>
        /// Reload the specified table.
        /// </summary>
        private IResponse Reload(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to reload");
            }

            using (request.Monitor(MonitorEventLevel.Information, "Reload", type: "Table", identity: tableName))
            {
                this.Database.ReloadTable(tableName);
                return ArribaResponse.Ok("Reloaded");
            }
        }

        /// <summary>
        /// Saves the specified table.
        /// </summary>
        private IResponse Save(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to save");
            }

            using (request.Monitor(MonitorEventLevel.Information, "Save", type: "Table", identity: tableName))
            {
                Table t = this.Database[tableName];

                // Verify before saving; don't save if inconsistent
                ExecutionDetails d = new ExecutionDetails();
                t.VerifyConsistency(VerificationLevel.Normal, d);

                if (d.Succeeded)
                {
                    t.Save();
                    return ArribaResponse.Ok("Saved");
                }
                else
                {
                    return ArribaResponse.Error("Table state inconsistent. Not saving. Restart server to reload. Errors: " + d.Errors);
                }
            }
        }

        /// <summary>
        /// Revokes access to a table. 
        /// </summary>
        private async Task<IResponse> Revoke(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to revoke permission on.");
            }

            var identity = await request.Request.ReadBodyAsync<SecurityIdentity>();
            if (String.IsNullOrEmpty(identity.Name))
            {
                return ArribaResponse.BadRequest("Identity name must not be empty");
            }

            PermissionScope scope;
            if (!Enum.TryParse<PermissionScope>(route["scope"], true, out scope))
            {
                return ArribaResponse.BadRequest("Unknown permission scope {0}", route["scope"]);
            }

            using (request.Monitor(MonitorEventLevel.Information, "RevokePermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                SecurityPermissions security = this.Database.Security(tableName);
                security.Revoke(identity, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }

            return ArribaResponse.Ok("Revoked");
        }

        /// <summary>
        /// Grants access to a table. 
        /// </summary>
        private async Task<IResponse> Grant(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to grant permission on.");
            }

            var identity = await request.Request.ReadBodyAsync<SecurityIdentity>();
            if (String.IsNullOrEmpty(identity.Name))
            {
                return ArribaResponse.BadRequest("Identity name must not be empty");
            }

            PermissionScope scope;
            if (!Enum.TryParse<PermissionScope>(route["scope"], true, out scope))
            {
                return ArribaResponse.BadRequest("Unknown permission scope {0}", route["scope"]);
            }

            using (request.Monitor(MonitorEventLevel.Information, "GrantPermission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                SecurityPermissions security = this.Database.Security(tableName);
                security.Grant(identity.Scope, identity.Name, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }

            return ArribaResponse.Ok("Granted");
        }

        private static string SanitizeIdentity(string rawIdentity)
        {
            if (String.IsNullOrEmpty(rawIdentity))
            {
                throw new ArgumentException("Identity must not be empty", "rawIdentity");
            }

            return rawIdentity.Replace("/", "\\");
        }
    }
}
