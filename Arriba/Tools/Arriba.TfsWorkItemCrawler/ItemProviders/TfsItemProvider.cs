using Arriba.Extensions;
using Arriba.Model.Column;
using Arriba.Structures;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Arriba.TfsWorkItemCrawler.ItemProviders
{
    public class TfsItemProvider : IItemProvider
    {
        internal const string EncryptedPasswordFilePathFormat = "{0}.Token.bin";
        private const string DefaultQuery = "SELECT [System.Id], [System.ChangedDate] FROM WorkItems WHERE [System.ChangedDate] > '@Start' AND [System.ChangedDate] < '@End' ORDER BY [System.ChangedDate] DESC";

        private string DatabaseUri { get; set; }
        private string Query { get; set; }
        private Dictionary<string, string> ColumnMappings { get; set; }

        private JsonSerializerSettings SerializerSettings { get; set; }
        private WorkItemStore Store { get; set; }

        public TfsItemProvider(CrawlerConfiguration config)
        {
            this.DatabaseUri = config.ItemDatabaseName;
            this.Query = config.ItemQuery ?? DefaultQuery;
            this.ColumnMappings = config.ColumnMappings;

            this.SerializerSettings = new JsonSerializerSettings();
            this.SerializerSettings.Converters.Add(new AttachmentCollectionJsonConverter());
            this.SerializerSettings.Converters.Add(new LinkCollectionJsonConverter());
            this.SerializerSettings.Converters.Add(new RevisionCollectionJsonConverter());

            // TODO: Figure out Personal Access Tokens (may only work for REST APIs)
            // https://www.visualstudio.com/en-us/docs/setup-admin/team-services/use-personal-access-tokens-to-authenticate

            // Connect to TFS, using encrypted credentials if found or the current user identity otherwise
            switch ((config.AuthenticationMode ?? String.Empty).ToLowerInvariant())
            {
                case "aad":
                    // https://www.visualstudio.com/en-us/docs/setup-admin/team-services/manage-organization-access-for-your-account-vs
                    Trace.WriteLine(string.Format("Connecting to '{0}' [AAD]...", this.DatabaseUri));
                    this.Store = new WorkItemStore(new TfsTeamProjectCollection(new Uri(this.DatabaseUri), new VssAadCredential()));
                    break;
                // Deprecated, but works.
                //case "alternate":
                //    Trace.WriteLine(string.Format("Connecting to '{0}' [Alternate]...", this.DatabaseUri));
                //    string encryptedBase64Password = File.ReadAllText(string.Format(EncryptedPasswordFilePathFormat, config.ConfigurationName));
                //    string unprotectedPassword = DecryptLocalUserPassword(encryptedBase64Password);

                //    //var credential = new TfsClientCredentials(new Microsoft.TeamFoundation.Client.WindowsCredential(new NetworkCredential(config.UserName, unprotectedPassword)));
                //    //credential.AllowInteractive = false;

                //    var tpc = new TfsTeamProjectCollection(new Uri(this.DatabaseUri), credential);
                //    this.Store = new WorkItemStore(tpc);
                //    break;
                case "integrated":
                case "":
                    Trace.WriteLine(string.Format("Connecting to '{0}' [Integrated Auth]...", this.DatabaseUri));
                    this.Store = new WorkItemStore(this.DatabaseUri);
                    break;
                default:
                    throw new NotImplementedException(string.Format("TfsItemProvider has no implementation for authenticationMode \"{0}\". Use 'aad', 'token', or 'integrated'.", config.AuthenticationMode));
            }

            // Debug Only: Get the fields list
            //IList<string> fields = GetStoreFields();
            //File.WriteAllLines(string.Format("{0}.Fields.txt", config.ArribaTable), fields);
        }

        #region TFS Online Password Storage
        public static int EncryptPassword(CrawlerConfiguration config)
        {
            Console.Write("Enter TFS Online Password to local user encrypt: ");
            string password = Console.ReadLine();
            if (String.IsNullOrEmpty(password)) return -1;

            string encryptedPasswordPath = string.Format(TfsItemProvider.EncryptedPasswordFilePathFormat, config.ConfigurationName);
            string encryptedPassword = TfsItemProvider.LocalUserEncryptPassword(password);
            File.WriteAllText(encryptedPasswordPath, encryptedPassword);

            Console.WriteLine("Encrypted Password written to '{0}'. Run Crawler to test.", encryptedPasswordPath);
            Console.WriteLine();

            return 0;
        }

        /// <summary>
        ///  Encrypt a TfsOnline password using local user secure storage and return
        ///  the protected password to store in a file referenced by config. 
        /// </summary>
        /// <remarks>
        ///  Reliable access to TFS online requires a stored web password, because otherwise
        ///  password UI will pop up every few months, blocking the crawler. The encrypted
        ///  value may be different per machine, so the config.json contains the file path
        ///  to a file with the encrypted value.
        ///  
        ///  On each machine running the crawler, run "Arriba.TfsWorkItemCrawler -password" as the user
        ///  which will run the crawler to store the credentials, and then copy the encrypted
        ///  value into the file path (ex: MS.EncryptedTfsPassword.txt).
        /// </remarks>
        /// <param name="value">Tfs Online Password to encrypt</param>
        /// <returns>Base64 encoded encrypted value to store in a file to use from a config</returns>
        public static string LocalUserEncryptPassword(string value)
        {
            byte[] unprotectedPasswordBytes = Encoding.UTF8.GetBytes(value);
            byte[] protectedPasswordBytes = ProtectedData.Protect(unprotectedPasswordBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedPasswordBytes);
        }

        /// <summary>
        ///  Decrypt the given Base64 encoded encrypted password using the local user secret,
        ///  so that it can be used to connect to TFS online securely.
        ///  
        ///  This API has to use string (not SecureString) because the TFS classes don't have
        ///  a SecureString way to pass the credentials on.
        /// </summary>
        /// <param name="encryptedBase64Password">The Base64 encoded encrypted password returned by LocalUserEncryptPassword</param>
        /// <returns>The raw password to use to connect to TFS.</returns>
        private static string DecryptLocalUserPassword(string encryptedBase64Password)
        {
            byte[] protectedPasswordBytes = Convert.FromBase64String(encryptedBase64Password);
            byte[] unprotectedPasswordBytes = ProtectedData.Unprotect(protectedPasswordBytes, null, DataProtectionScope.CurrentUser);
            string unprotectedPassword = Encoding.UTF8.GetString(unprotectedPasswordBytes);
            return unprotectedPassword;
        }
        #endregion

        #region IItemProvider
        public IList<ColumnDetails> GetColumns()
        {
            List<ColumnDetails> columns = new List<ColumnDetails>();
            foreach(FieldDefinition column in this.Store.FieldDefinitions)
            {
                columns.Add(new ColumnDetails(column.Name, MapType(column), null, null, column.Name == "ID"));
            }
            return columns;
        }

        private static string MapType(FieldDefinition column)
        {
            if (column.Name == "Attachements" || column.Name == "Links") return "json";

            switch(column.FieldType)
            {
                case FieldType.Boolean:
                    return "bool";
                case FieldType.Integer:
                    return "int";
                case FieldType.History:
                    return "json";
                case FieldType.PicklistInteger:
                case FieldType.PicklistString:
                case FieldType.PlainText:
                case FieldType.TreePath:
                case FieldType.Internal:
                    return "string";
                default:
                    return column.FieldType.ToString();
            }
        }

        public List<ItemIdentity> GetItemsChangedBetween(DateTime start, DateTime end)
        {
            List<ItemIdentity> result = new List<ItemIdentity>();

            if (!this.Query.Contains("@Start") || !this.Query.Contains("@End")) throw new ArgumentException(String.Format("Query file '{0}' did not contain '@Start' and '@End' for the Crawler to request only a subset of items. Stopping.", this.Query));

            DateTime lastLoaded = start;
            TimeSpan currentIntervalSize = end - start;

            while (lastLoaded < end)
            {
                // Get items until the end or up to the smallest interval which has worked, whichever is smaller
                TimeSpan remainingInterval = end - lastLoaded;
                if (currentIntervalSize > remainingInterval) currentIntervalSize = remainingInterval;

                // Build a query for that range
                DateTime nextToLoad = lastLoaded.Add(currentIntervalSize);
                string resolvedQuery = this.Query.Replace("@Start", lastLoaded.ToString("u")).Replace("@End", nextToLoad.ToString("u"));

                if (ExtractOneSet(resolvedQuery, result) != null)
                {
                    // If successful, we now only need to get items after the end of this interval (if we didn't get everything)
                    lastLoaded = nextToLoad;
                }
                else
                {
                    // If unsuccessful, halve the interval we get at one time and retry
                    currentIntervalSize = new TimeSpan(currentIntervalSize.Ticks / 2);
                    if (currentIntervalSize.TotalMinutes < 1) throw new InvalidOperationException("Unable to crawl from TFS successfully even with only a one minute interval. Stopping.");

                    Trace.WriteLine(string.Format("TFS couldn't return items between {0} and {1}. Shortening interval to {2}.", lastLoaded.ToString("u"), nextToLoad.ToString("u"), currentIntervalSize.ToFriendlyString()));
                }
            }

            return result;
        }

        private List<ItemIdentity> ExtractOneSet(string query, List<ItemIdentity> result)
        {
            Query q = new Query(this.Store, query, null, false);

            // Ask for ID only - we don't need anything else
            q.DisplayFieldList.Clear();
            q.DisplayFieldList.Add(this.Store.FieldDefinitions[CoreField.Id]);
            q.DisplayFieldList.Add(this.Store.FieldDefinitions[CoreField.ChangedDate]);

            // Sort by ChangedDate ascending for restartability
            q.SortFieldList.Clear();
            q.SortFieldList.Add(this.Store.FieldDefinitions[CoreField.ChangedDate].Name, SortType.Ascending);

            try
            {
                // Run the query
                ICancelableAsyncResult car = q.BeginQuery();
                WorkItemCollection items = q.EndQuery(car);
                // Set PageSize to Maximum, since we'll be reading all of them anyway.
                items.PageSize = 200;

                // Record the IDs to load
                int count = items.Count;
          
                for (int i = 0; i < count; ++i)
                {
                    WorkItem item = items[i];
                    result.Add(new ItemIdentity(item.Id, ItemProviderUtilities.CanonicalizeDateTime(item.ChangedDate)));
                }
            }
            catch (VerbatimMessageException ex)
            {
                Match match = Regex.Match(ex.Message, @"VS402337: The number of work items returned exceeds the size limit of (?<BatchSize>\d+)\.");
                if (match.Success)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }

            return result;
        }

        public DataBlock GetItemBlock(IEnumerable<ItemIdentity> items, IEnumerable<string> columnNames)
        {
            // Build a query for them
            string query = String.Format("SELECT [System.Id] FROM WorkItems WHERE [System.Id] IN ({0})", String.Join(", ", items.Select((ii) => ii.ID)));
            Query q = new Query(this.Store, query, null, false);

            // Ask for fields we'll be populating to avoid additional network requests
            q.DisplayFieldList.Clear();
            foreach (string columnName in columnNames)
            {
                if (this.Store.FieldDefinitions.Contains(columnName))
                {
                    q.DisplayFieldList.Add(this.Store.FieldDefinitions[columnName]);
                }
                else if (columnName.Equals("attachments", StringComparison.OrdinalIgnoreCase))
                {
                    // Add AttachedFileCount to greatly speed loading attachments (if this is the only collection; rare)
                    q.DisplayFieldList.Add(this.Store.FieldDefinitions[CoreField.AttachedFileCount]);
                }
            }

            // Run the query
            ICancelableAsyncResult car = q.BeginQuery();
            WorkItemCollection itemCollection = q.EndQuery(car);

            // Copy the item field values into a DataBlock and track the last cutoff per group
            DataBlock result = new DataBlock(columnNames, items.Count());
            for (int itemIndex = 0; itemIndex < result.RowCount; ++itemIndex)
            {
                WorkItem item = itemCollection[itemIndex];

                int fieldIndex = 0;
                foreach(string columnName in columnNames)
                { 
                    try
                    {
                        result[itemIndex, fieldIndex] = ItemProviderUtilities.Canonicalize(GetFieldValue(item, columnName));
                    }
                    catch (Exception ex)
                    {
                        result[itemIndex, fieldIndex] = null;
                        Trace.WriteLine(String.Format("Error Getting '{0}' from item {1}. Skipping field. Detail: {2}", columnName, item.Id, ex.ToString()));
                    }

                    fieldIndex++;
                }
            }

            return result;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (this.Store != null)
            {
                this.Store.TeamProjectCollection.Dispose();
                this.Store = null;
            }
        }
        #endregion

        #region Field Handling
        private object GetFieldValue(WorkItem item, string fieldName)
        {
            // Debug Only: Get all field values for this item.
            //Dictionary<string, object> allValues = GetAllItemFields(item);

            switch (fieldName.ToLowerInvariant())
            {
                case "cid":
                    return String.Format("{0}.{1}", item.Project.Name, item.Id);

                case "project":
                    return item.Project.Name;

                case "attachments":
                    return GetAttachments(item);

                case "links":
                    return GetLinks(item);

                case "fullhistory":
                    return GetFullHistory(item);

                default:
                    {
                        if (item.Fields.Contains(fieldName))
                        {
                            return item.Fields[fieldName].Value;
                        }
                        else if (this.ColumnMappings.ContainsKey(fieldName))
                        {
                            string realFieldName = this.ColumnMappings[fieldName];

                            if (item.Fields.Contains(realFieldName))
                            {
                                return item.Fields[realFieldName].Value;
                            }
                        }

                        return null;
                    }
            }
        }

        private string GetAttachments(WorkItem item)
        {
            if (item.AttachedFileCount == 0) return String.Empty;
            return Newtonsoft.Json.JsonConvert.SerializeObject(item.Attachments, this.SerializerSettings);
        }

        private string GetLinks(WorkItem item)
        {
            LinkCollection links = item.Links;
            if (links.Count == 0) return String.Empty;
            return Newtonsoft.Json.JsonConvert.SerializeObject(links, this.SerializerSettings);
        }

        private string GetFullHistory(WorkItem item)
        {
            RevisionCollection revisions = item.Revisions;
            return Newtonsoft.Json.JsonConvert.SerializeObject(revisions, this.SerializerSettings);
        }

        private Dictionary<string, object> GetAllItemFields(WorkItem item)
        {
            Dictionary<string, object> fieldValues = new Dictionary<string, object>();

            foreach (Field field in item.Fields)
            {
                fieldValues[field.Name] = field.Value;
            }

            return fieldValues;
        }

        public IList<string> GetStoreFields()
        {
            List<string> allFields = new List<string>();

            foreach (FieldDefinition field in this.Store.FieldDefinitions)
            {
                allFields.Add(String.Format("{0}\t{1}", field.Name, field.FieldType));
            }

            allFields.Sort();
            return allFields;
        }
        #endregion
    }
}
