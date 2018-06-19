// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using XForm.Extensions;
using XForm.IO.StreamProvider;

namespace XForm.IO
{
    /// <summary>
    ///  ItemVersions contains the list of versions found for a given item (Source, Table, Report).
    /// </summary>
    public class ItemVersions
    {
        public LocationType LocationType { get; set; }
        public string Name { get; set; }
        public List<ItemVersion> Versions { get; set; }

        public ItemVersions(LocationType locationType, string name)
        {
            this.LocationType = locationType;
            this.Name = name;
            this.Versions = new List<ItemVersion>();
        }

        public void AddVersion(CrawlType crawlType, DateTime asOfDate)
        {
            this.Versions.Add(new ItemVersion(this, crawlType, asOfDate));
        }

        public void Merge(ItemVersions other)
        {
            if (this.LocationType != other.LocationType || this.Name != other.Name) throw new ArgumentException();

            HashSet<ItemVersion> allVersions = new HashSet<ItemVersion>(this.Versions);
            allVersions.UnionWith(other.Versions);
            this.Versions = new List<ItemVersion>(allVersions.OrderByDescending((v) => v.AsOfDate));
        }

        public ItemVersion LatestBeforeCutoff(CrawlType crawlType, DateTime cutoff)
        {
            return this.Versions.LastOrDefault((v) => (v.CrawlType == crawlType && v.AsOfDate <= cutoff));
        }

        public IEnumerable<ItemVersion> VersionsInRange(CrawlType crawlType, DateTime startDateTime, DateTime asOfDateTime, bool startInclusive = false)
        {
            if (startInclusive)
            {
                return this.Versions.Where((v) => (v.CrawlType == crawlType && v.AsOfDate >= startDateTime && v.AsOfDate <= asOfDateTime));
            }
            else
            {
                return this.Versions.Where((v) => (v.CrawlType == crawlType && v.AsOfDate > startDateTime && v.AsOfDate <= asOfDateTime));
            }
        }
    }

    public class ItemVersion
    {
        public LocationType LocationType { get; set; }
        public string Name { get; set; }
        public CrawlType CrawlType { get; set; }
        public DateTime AsOfDate { get; set; }

        public ItemVersion(ItemVersions container, CrawlType crawlType, DateTime asOfDate)
        {
            this.LocationType = container.LocationType;
            this.Name = container.Name;
            this.CrawlType = crawlType;
            this.AsOfDate = asOfDate;
        }

        public ItemVersion(LocationType locationType, string tableName, CrawlType crawlType, DateTime asOfDate)
        {
            this.LocationType = locationType;
            this.Name = tableName;
            this.CrawlType = crawlType;
            this.AsOfDate = asOfDate;
        }

        public bool Equals(ItemVersion other)
        {
            if (other == null) return false;

            return this.LocationType.Equals(other.LocationType)
                && this.Name.Equals(other.Name)
                && this.CrawlType.Equals(other.CrawlType)
                && this.AsOfDate.Equals(other.AsOfDate);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ItemVersion);
        }

        public override int GetHashCode()
        {
            return LocationType.GetHashCode() ^ Name.GetHashCode() ^ CrawlType.GetHashCode() ^ AsOfDate.GetHashCode();
        }

        public string Path => System.IO.Path.Combine(this.LocationType.ToString(), this.Name, this.CrawlType.ToString(), this.AsOfDate.ToString(StreamProviderExtensions.DateTimeFolderFormat));
    }
}
