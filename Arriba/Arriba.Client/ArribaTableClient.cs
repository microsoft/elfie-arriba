// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Structures;
using Arriba.Types;

namespace Arriba.Client
{
    public class ArribaTableClient
    {
        private ArribaClient _client;
        private string _tableName;

        internal ArribaTableClient(ArribaClient arribaClient)
        {
            _client = arribaClient;
        }

        internal ArribaTableClient(ArribaClient arribaClient, string tableName)
        {
            _client = arribaClient;
            _tableName = tableName;
        }

        public async Task<TableInformation> GetTableInformationAsync()
        {
            return await _client.SendAsync<TableInformation>(HttpMethod.Get, String.Format("table/{0}", _tableName));
        }

        #region Table Management
        internal async Task CreateAsync(CreateTableRequest request)
        {
            var resp = await _client.SendObjectAsync(HttpMethod.Post, "table/", value: request);
            await resp.EnsureArribaSuccess();
            _tableName = request.TableName;
            return;
        }

        public async Task AddColumnsAsync(IEnumerable<ColumnDetails> columns)
        {
            var resp = await _client.SendObjectAsync(
                HttpMethod.Post,
                String.Format("table/{0}/addcolumns", _tableName), null, columns);

            await resp.EnsureArribaSuccess();
            return;
        }

        public async Task SaveAsync()
        {
            var resp = await _client.SendAsync(HttpMethod.Get, String.Format("table/{0}/save", _tableName));
            await resp.EnsureArribaSuccess();
            return;
        }

        public async Task DeleteAsync()
        {
            var resp = await _client.SendAsync(HttpMethod.Delete, String.Format("table/{0}", _tableName));
            await resp.EnsureArribaSuccess();
            return;
        }
        #endregion

        #region Permissions
        public async Task SetPermissionsAsync(SecurityPermissions permissions)
        {
            var resp = await _client.SendObjectAsync(
                HttpMethod.Post,
                String.Format("table/{0}/permissions", _tableName),
                value: permissions);

            await resp.EnsureArribaSuccess();
            return;
        }

        public Task<SecurityPermissions> GetPermissionsAsync()
        {
            return _client.GetAsync<SecurityPermissions>(String.Format("table/{0}/permissions", _tableName));
        }

        public async Task RemovePermissionsAsync(PermissionScope permissionsScope, string identity)
        {
            var resp = await _client.SendObjectAsync(
                HttpMethod.Delete,
                String.Format("table/{0}/permissions/{1}/{2}", _tableName, permissionsScope, identity));

            await resp.EnsureArribaSuccess();
            return;
        }

        public async Task GrantPermissionsAsync(PermissionScope scope, IdentityScope identityType, string identity)
        {
            var resp = await _client.SendObjectAsync(
               HttpMethod.Post,
               String.Format("table/{0}/permissions/{1}", _tableName, scope.ToString().ToLowerInvariant()),
               value: SecurityIdentity.Create(identityType, identity));

            await resp.EnsureArribaSuccess();
            return;
        }

        public async Task RevokePermissionsAsync(PermissionScope scope, IdentityScope identityType, string identity)
        {
            var resp = await _client.SendObjectAsync(HttpMethod.Delete, String.Format("table/{0}/permissions/{1}", _tableName, scope.ToString().ToLowerInvariant()), value: SecurityIdentity.Create(identityType, identity));
            await resp.EnsureArribaSuccess();
            return;
        }
        #endregion

        #region Import [AddOrUpdate]
        public async Task ImportDataBlock(DataBlock block)
        {
            var resp = await _client.SendObjectAsync(
               HttpMethod.Post,
               String.Format("table/{0}", _tableName), new { type = "block" }, block);

            await resp.EnsureArribaSuccess();
            return;
        }

        public async Task ImportFileAsync(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                await this.ImportFileAsync(fs, Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant());
            }
        }

        public async Task ImportFileAsync(Stream stream, string typeOfFile)
        {
            // Reset the Stream (in case it was just built by something)
            stream.Seek(0, SeekOrigin.Begin);

            string contentType = ContentTypeForFileExtension(typeOfFile);
            var resp = await _client.SendStreamAsync(HttpMethod.Post, String.Format("table/{0}", _tableName), parameters: new { type = typeOfFile }, value: stream, contentType: contentType);
            await resp.EnsureArribaSuccess();
            return;
        }

        private static string ContentTypeForFileExtension(string extension)
        {
            switch (extension.TrimStart('.').ToLowerInvariant())
            {
                case "csv":
                    return "text/csv";
                case "json":
                    return "application/json";
                default:
                    return "text/plain";
            }
        }
        #endregion

        public async Task<SelectResult> Select(SelectQuery query)
        {
            return await _client.SendObjectAsync<SelectResult>(HttpMethod.Post, String.Format("table/{0}?action=select", _tableName), value: query);
        }

        public async Task<Stream> SelectAsFormat(SelectQuery query, string format)
        {
            return await _client.RequestStreamAsync(HttpMethod.Post, String.Format("table/{0}?action=select&fmt={1}", _tableName, format), value: query);
        }

        public async Task<AggregationResult> Aggregate(AggregationQuery query)
        {
            return await _client.SendObjectAsync<AggregationResult>(HttpMethod.Post, String.Format("table/{0}?action=aggregate", _tableName), value: query);
        }

        public async Task<DistinctResult> Distinct(DistinctQuery query)
        {
            return await _client.SendObjectAsync<DistinctResult>(HttpMethod.Post, String.Format("table/{0}?action=distinct", _tableName), value: query);
        }

        public async Task<U> Query<U>(string action, IQuery<U> query)
        {
            return await _client.SendObjectAsync<U>(HttpMethod.Post, String.Format("table/{0}?action={1}", _tableName, action), value: query);
        }

        public async Task<int> Delete(IExpression where)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["q"] = where;

            return await _client.SendAsync<int>(HttpMethod.Post, String.Format("table/{0}?action=delete", _tableName), parameters: parameters);
        }
    }
}
