// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.Elfie.EndToEnd
{
    internal class PopularityDetails
    {
        public string PackageName;
        public long DownloadCount;
        public int TotalMemberCount;
        public int PublicTypeCount;
        public int MergedTypeCount;
        public long AddReferenceAddedBytes;
    }

    public class PopularityAndSizeReport
    {
        private AddReferenceDatabase _addReferenceDB;
        private List<PopularityDetails> _results;
        private long _downloadTotal;

        public PopularityAndSizeReport()
        {
            _addReferenceDB = new AddReferenceDatabase(ArdbVersion.Current);
            _results = new List<PopularityDetails>();
        }

        public void Add(PackageDatabase source)
        {
            long ardbBytesBefore = _addReferenceDB.Bytes;

            DatabaseAddResult result = _addReferenceDB.AddUniqueMembers(source);

            PopularityDetails details = new PopularityDetails();
            details.PackageName = source.Identity.PackageName;
            details.DownloadCount = source.Identity.DownloadCount;
            details.TotalMemberCount = source.Count;
            details.PublicTypeCount = result.PublicTypeCount;
            details.MergedTypeCount = result.MergedTypeCount;
            details.AddReferenceAddedBytes = _addReferenceDB.Bytes - ardbBytesBefore;

            _results.Add(details);
            _downloadTotal += details.DownloadCount;
        }

        public void Write(string csvFilePath)
        {
            using (StreamWriter writer = new StreamWriter(csvFilePath, false))
            {
                writer.WriteLine("PackageName,DownloadCount,TotalMemberCount,PublicTypeCount,MergedTypeCount,AddReferenceBytes,CumulativeDownloadPercentage,CumulativePublicTypeCount,CumulativeMergedTypeCount,CumulativeAddReferenceBytes");

                long downloadsSoFar = 0;
                int publicTypeCountSoFar = 0;
                int mergedTypeCountSoFar = 0;
                long addReferenceBytesSoFar = 0;

                for (int i = 0; i < _results.Count; ++i)
                {
                    PopularityDetails d = _results[i];
                    downloadsSoFar += d.DownloadCount;
                    publicTypeCountSoFar += d.PublicTypeCount;
                    mergedTypeCountSoFar += d.MergedTypeCount;
                    addReferenceBytesSoFar += d.AddReferenceAddedBytes;

                    writer.Write(d.PackageName);
                    writer.Write(",");
                    writer.Write(d.DownloadCount);
                    writer.Write(",");
                    writer.Write(d.TotalMemberCount);
                    writer.Write(",");
                    writer.Write(d.PublicTypeCount);
                    writer.Write(",");
                    writer.Write(d.MergedTypeCount);
                    writer.Write(",");
                    writer.Write(d.AddReferenceAddedBytes);
                    writer.Write(",");
                    writer.Write((downloadsSoFar / (double)_downloadTotal).ToString("n4"));
                    writer.Write(",");
                    writer.Write(publicTypeCountSoFar);
                    writer.Write(",");
                    writer.Write(mergedTypeCountSoFar);
                    writer.Write(",");
                    writer.Write(addReferenceBytesSoFar);

                    writer.WriteLine();
                }
            }
        }
    }
}
