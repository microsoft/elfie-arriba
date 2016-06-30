// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Configuration;

using CommandLine;

using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.Elfie.Merger
{
    public class MergerOptions
    {
        public const string Usage = @"
ex: Elfie.Merger -p ""C:\Download\NuGet.All"" -o ""C:\Download""
ex: Elfie.Merger -p ""C:\Download\NuGet.All"" -o ""C:\Download"" -dl 0.98 --wpo --wmtl";

        [Option('p', Required = true, HelpText = "Path to Folder Path containing IDXes to merge or Text File list of IDX paths in ranked order.")]
        public string PathToMerge { get; set; }

        [Option('o', Required = true, HelpText = "Output Directory within which to write merged database.")]
        public string OutputPath { get; set; }

        [Option('v', HelpText = "Version Number to write into database. Defaults to the UTC Date [yyyyMMdd].")]
        public int DatabaseVersion { get; set; }

        [Option("ln", HelpText = "Alternate Use/Error log folder name. Logs are in %LocalAppData%\\LogName.")]
        public string LogName { get; set; }

        [Option("dl", HelpText = "Download Percentage to cover with included Packages. Ignored if file list passed.")]
        public double DownloadPercentage { get; set; }

        [Option("wpo", HelpText = "Whether to write a log of which members were included for each package.")]
        public bool WithPackageOutcomes { get; set; }

        [Option("wmtl", HelpText = "Whether to write a log of the merged member tree with the package associated with each member.")]
        public bool WithMergedTreeLog { get; set; }

        [Option("exclude", HelpText = "Excluded Package Names, ';' delimited. Defaults to App.Config excluded package names if not set.")]
        public string ExcludedPackageNames { get; set; }

        [Option("ddf", HelpText = "Disable Duplicate Filtering for specified package names, ';' delimited. Defaults to App.Config disable duplicate filtering package names if not set.")]
        public string DisableDuplicateFilteringPackageNames { get; set; }

        [Option('f', "format", HelpText = "Version of add package reference database format to emit. Valid values to specify are V1 and Current (the default).", Default = ArdbVersion.Current)]
        public ArdbVersion Version { get; set; }

        public MergerOptions()
        {
            DownloadPercentage = 0.98;

            DateTime utcNow = DateTime.UtcNow;
            DatabaseVersion = utcNow.Year * 10000 + utcNow.Month * 100 + utcNow.Day;

            this.ExcludedPackageNames = ConfigurationManager.AppSettings["Excluded_PackageNames"];
            this.DisableDuplicateFilteringPackageNames = ConfigurationManager.AppSettings["DisableDuplicateFiltering_PackageNames"];
        }
    }
}
