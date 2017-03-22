using Arriba.Model;
using Arriba.Model.Column;
using System;
using System.Collections.Generic;
using Arriba.Model.Security;

namespace Arriba.TfsWorkItemCrawler
{
    public class CrawlerConfiguration
    {
        public string ArribaTable { get; set; }
        public long ItemCountLimit { get; set; }
        public List<ColumnDetails> Columns { get; set; }

        public string ItemProvider { get; set; }
        public string ItemDatabaseName { get; set; }
        public string ItemQueryFile { get; set; }
        public string AllItemsQuery { get; set; }

        public string TfsOnlineUserName { get; set; }
        public string TfsOnlineEncryptedPasswordFilePath { get; set; }
        public bool UseAADForTFSAuth { get; set; }

        public Dictionary<string, string> ColumnMappings { get; set; }


        public IReadOnlyList<string> ColumnNames
        {
            get
            {
                List<string> columnNames = new List<string>();
                foreach(ColumnDetails details in this.Columns)
                {
                    columnNames.Add(details.Name);
                }
                return columnNames;
            }
        }

        public CrawlerConfiguration()
        {
            this.Columns = new List<ColumnDetails>();
            this.ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public SecurityPermissions LoadPermissions()
        {
            throw new NotImplementedException();
        }
    }
}
