using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace V5.Serialization
{
    internal class CacheEntry
    {
        public string Identifier { get; set; }
        public IBinarySerializable Item { get; set; }

        public DateTime WhenLoaded { get; set; }
        public long SizeBytes { get; set; }
        public int UseCount { get; set; }
    }

    public class CachedLoader : IDisposable
    {
        public string BasePath { get; private set; }
        public long MemoryLimitBytes { get; private set; }
        public long CurrentUseBytes { get; private set; }

        private Dictionary<string, CacheEntry> CachedItems { get; set; }

        public CachedLoader(string basePath, long memoryLimitBytes = -1)
        {
            this.BasePath = basePath;
            this.MemoryLimitBytes = memoryLimitBytes;

            this.CachedItems = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        }

        public T Get<T>(string identifier) where T : IBinarySerializable, new()
        {
            lock (this.CachedItems)
            {
                CacheEntry entry;

                // If the item is already cached, return it
                if (this.CachedItems.TryGetValue(identifier, out entry))
                {
                    entry.UseCount++;
                    return (T)entry.Item;
                }

                // Otherwise, load and return it
                entry = Load<T>(identifier);
                this.CachedItems[identifier] = entry;
                this.CurrentUseBytes += entry.SizeBytes;

                return (T)entry.Item;
            }
        }

        public void Register<T>(string identifier, T item) where T : IBinarySerializable, new()
        {
            CacheEntry entry = new CacheEntry();
            entry.Identifier = identifier;
            entry.WhenLoaded = DateTime.UtcNow;
            entry.UseCount = 1;
            entry.Item = item;

            // ISSUE: Size unknown.

            this.CachedItems[identifier] = entry;
        }

        private CacheEntry Load<T>(string identifier) where T : IBinarySerializable, new()
        {
            CacheEntry entry = new CacheEntry();
            entry.Identifier = identifier;
            entry.WhenLoaded = DateTime.UtcNow;
            entry.UseCount = 1;

            T item = new T();

            string filePath = Path.Combine(this.BasePath, identifier);
            using (FileStream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                entry.SizeBytes = s.Length;
                this.ReleaseItems(this.MemoryLimitBytes - s.Length);

                BinaryReader reader = new BinaryReader(s);
                item.ReadBinary(reader, s.Length);
            }

            entry.Item = item;
            return entry;
        }

        public void Save(string identifier)
        {
            CacheEntry entry;
            if (this.CachedItems.TryGetValue(identifier, out entry))
            {
                this.Save(entry);
            }
        }

        private void Save(CacheEntry entry)
        {
            if (!entry.Item.PrepareToWrite()) return;

            string filePath = Path.Combine(this.BasePath, entry.Identifier);
            string temporaryPath = Path.ChangeExtension(filePath, ".new");

            // Ensure the containing folder exists
            string serializationDirectory = Path.GetDirectoryName(filePath);
            if (!String.IsNullOrEmpty(serializationDirectory)) Directory.CreateDirectory(serializationDirectory);

            // Serialize the item
            long lengthWritten = 0;
            FileStream s = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.Delete);
            using (BinaryWriter writer = new BinaryWriter(s))
            {
                entry.Item.WriteBinary(writer);
                lengthWritten = s.Position;
            }

            if (lengthWritten == 0)
            {
                // If nothing was written, delete the file
                File.Delete(temporaryPath);
                File.Delete(filePath);
            }
            else
            {
                // Otherwise, replace the previous official file with the new one
                File.Move(temporaryPath, filePath);
            }
        }

        public void ReleaseItems(long desiredMemoryUseBytes)
        {
            if (this.CurrentUseBytes <= desiredMemoryUseBytes) return;

            List<CacheEntry> removed = new List<CacheEntry>();

            foreach(CacheEntry entry in this.CachedItems.Values.OrderBy((entry) => entry.UseCount / entry.SizeBytes))
            {
                if (entry.Item.PrepareToWrite())
                {
                    Save(entry);
                }

                removed.Add(entry);                
                this.CurrentUseBytes -= entry.SizeBytes;

                if (this.CurrentUseBytes <= desiredMemoryUseBytes) break;
            }

            foreach(CacheEntry entry in removed)
            {
                this.CachedItems.Remove(entry.Identifier);
            }
        }

        public void Dispose()
        {
            ReleaseItems(0);
            this.CachedItems = null;
        }
    }
}
