// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Arriba.Extensions;
using Arriba.Model.Query;
using Arriba.Serialization;

namespace Arriba.Model
{
    public class Database
    {
        public const string SystemTablePrefix = "arriba";

        protected readonly object _tableLock = new object();
        private readonly Dictionary<string, Lazy<Table>> _tables = new Dictionary<string, Lazy<Table>>();

        public Database()
        { }

        public virtual T Query<T>(IQuery<T> query)
        {
            return this[query.TableName].Query(query);
        }

        public Table this[string tableName]
        {
            get
            {
                return this.GetOrLoadTable(tableName);
            }
        }

        public void DropTable(string tableName)
        {
            lock (_tableLock)
            {
                if (Table.Exists(tableName))
                {
                    Table.Drop(tableName);
                    _tables.Remove(tableName);
                }
                else
                {
                    throw new ArribaException("Table does not exist");
                }
            }
        }

        public Table AddTable(string tableName, long itemCountLimit)
        {
            if (tableName == null) throw new ArgumentNullException("tableName");

            if (tableName.StartsWith(SystemTablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(StringExtensions.Format("Table prefix {0} is reserved for system tables", SystemTablePrefix));
            }

            return AddTableInternal(tableName, itemCountLimit);
        }

        internal Table AddTableInternal(string tableName, long itemCountLimit)
        {
            if (this.TableExists(tableName)) throw new ArribaException(StringExtensions.Format("Table '{0}' already exists; it can't be added.", tableName));

            Table newTable = new Table(tableName, itemCountLimit);

            lock (_tableLock)
            {
                // Add or Replace Lazy entry; it might be a Lazy -> null from trying to load the Table before it existed
                _tables[tableName] = new Lazy<Table>(() => newTable);
            }

            return newTable;
        }

        public bool GetOrAddTable(string tableName, byte partitionBits, out Table table)
        {
            if (this.TableExists(tableName))
            {
                table = this.GetOrLoadTable(tableName);
                return false;
            }

            table = this.AddTable(tableName, partitionBits);
            return true;
        }

        internal bool GetOrAddTableInternal(string tableName, byte partitionBits, out Table table)
        {
            if (this.TableExists(tableName))
            {
                table = this.GetOrLoadTable(tableName);
                return false;
            }

            table = this.AddTableInternal(tableName, partitionBits);
            return true;
        }


        private bool IsTableLoaded(string tableName)
        {
            return _tables.ContainsKey(tableName) && _tables[tableName].IsValueCreated;
        }

        public bool TableExists(string tableName)
        {
            return this.IsTableLoaded(tableName) || this.TableExistsOnDisk(tableName);
        }

        private bool TableExistsOnDisk(string tableName)
        {
            return this.TableNames.Contains(tableName, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> TableNames
        {
            get
            {
                return BinarySerializable.EnumerateDirectoriesUnder("Tables").Select(p => Path.GetFileName(p)).Where((name) => Table.Exists(name));
            }
        }

        private Table GetOrLoadTable(string tableName)
        {
            Lazy<Table> tableLazy = null;

            lock (_tableLock)
            {
                if (!_tables.TryGetValue(tableName, out tableLazy))
                {
                    tableLazy = new Lazy<Table>(() =>
                    {
                        if (!Table.Exists(tableName)) return null;

                        Table t = new Table();
                        t.Load(tableName);
                        return t;
                    }, LazyThreadSafetyMode.ExecutionAndPublication);

                    _tables.Add(tableName, tableLazy);
                }
            }

            return tableLazy.Value;
        }

        public virtual void ReloadTable(string tableName)
        {
            lock (_tableLock)
            {
                _tables[tableName] = new Lazy<Table>(() =>
                {
                    Table t = new Table();
                    t.Load(tableName);
                    return t;
                });
            }
        }
        
        public virtual void UnloadTable(string tableName)
        {
            lock(_tableLock)
            {
                _tables.Remove(tableName);
            }
        }

        public virtual void UnloadAll()
        {
            lock (_tableLock)
            {
                _tables.Clear();
            }
        }

        public void EnsureLoaded(string tableName)
        {
            this.GetOrLoadTable(tableName);
        }
    }
}
