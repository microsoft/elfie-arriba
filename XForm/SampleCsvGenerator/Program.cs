// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using SampleCsvGenerator.Model;

namespace SampleCsvGenerator
{
    internal class Program
    {
        private const int RandomSeed = 5;
        private const int UserCount = 250 * 1000;
        private const int NumberOfDays = 3;

        private static string Usage = @"Usage: SampleCsvGenerator [xform|single] [outPath] [rowCount?]
  - Pass 'xform' argument to generate XForm demo sample data.
  - Pass 'single' to generate a single sample CSV.";

        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine(Usage);
                return;
            }

            string mode = args[0].ToLowerInvariant();
            string outputPath = Path.GetFullPath(args[1]);
            int rowCount = (args.Length > 2 ? int.Parse(args[2]) : 10 * 1000 * 1000);

            if (mode == "xform")
            {
                GenerateXFormSample(outputPath, rowCount);
            }
            else if (mode == "single")
            {
                Generate_WebRequestSample(outputPath, rowCount);
            }
            else
            {
                Console.WriteLine($"Unknown Mode: '{mode}'");
                Console.WriteLine(Usage);
                return;
            }

            Console.WriteLine("Done.");
        }

        private static void Generate_WebRequestSample(string basePath, int eventCount)
        {
            Random r = new Random(RandomSeed);
            DateTime asOfDate = DateTime.UtcNow.Date;
            WebRequestGenerator generator;

            // Generate a set of users and write them out [for a week ago]
            UserGenerator userGenerator = new UserGenerator(r, asOfDate);
            List<User> users = userGenerator.Next(UserCount);

            // Generate the requested sample file
            generator = new WebRequestGenerator(users, r, asOfDate, 1000);
            generator.Issue = new PortRangeBlocked(asOfDate.AddMinutes(1), asOfDate.AddMinutes(180), 11450, 11480);
            BuildWebRequests(basePath, "Sample", generator, eventCount, WebRequestWriteMode.UserIdentityOnly);

            Console.WriteLine("Done.");
        }

        private static void GenerateXFormSample(string basePath, int eventCount)
        {
            Random r = new Random(RandomSeed);
            DateTime asOfDate = DateTime.UtcNow.Date;
            String8Block block = new String8Block();
            WebRequestGenerator generator;

            string path;

            // Generate a set of users and write them out [for a week ago]
            asOfDate = asOfDate.AddDays(-8);
            path = Path.Combine(basePath, $"Users.{asOfDate:yyyyMMdd}.r{RandomSeed}.{UserCount}.csv");
            Console.WriteLine($"Writing {path}...");
            UserGenerator userGenerator = new UserGenerator(r, asOfDate);
            List<User> users = userGenerator.Next(UserCount);
            using (ITabularWriter writer = TabularFactory.BuildWriter(path))
            {
                foreach (User user in users)
                {
                    user.WriteTo(writer, block);
                }
            }

            File.SetLastWriteTimeUtc(path, asOfDate);

            // Generate WebRequest Data [for a week ago]
            generator = new WebRequestGenerator(users, r, asOfDate, (eventCount < 1001 ? 10 : 100));
            BuildWebRequests(basePath, "WebRequest", generator, eventCount, WebRequestWriteMode.All);

            asOfDate = asOfDate.AddDays(8);

            // Generate batches of WebRequest sample data [current]
            for (int day = 0; day < NumberOfDays; ++day)
            {
                generator = new WebRequestGenerator(users, r, asOfDate, (eventCount < 1001 ? 10 : 100));
                if (day == 0) generator.Issue = new PremiumUserOutage(asOfDate.AddMinutes(18), asOfDate.AddMinutes(104), r);
                BuildWebRequests(basePath, "WebRequest", generator, eventCount, WebRequestWriteMode.UserIdentityOnly);
                asOfDate = asOfDate.AddDays(-1);
            }

            Console.WriteLine("Done.");
        }

        private static void BuildWebRequests(string basePath, string fileNamePrefix, WebRequestGenerator generator, int eventCount, WebRequestWriteMode mode)
        {
            DateTime asOfDate = generator.Current;
            String8Block block = new String8Block();

            string path = Path.Combine(basePath, $"{fileNamePrefix}.{asOfDate:yyyyMMdd}.r5.{eventCount}.csv");
            Console.WriteLine($"Writing {path}...");

            using (ITabularWriter writer = TabularFactory.BuildWriter(path))
            {
                for (int i = 0; i < eventCount; ++i)
                {
                    WebRequest request = generator.Next();
                    request.WriteTo(writer, block, i, mode);
                }
            }

            File.SetLastWriteTimeUtc(path, asOfDate);
        }
    }
}
