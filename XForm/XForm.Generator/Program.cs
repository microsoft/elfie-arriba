// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Generator.Model;

namespace XForm.Generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("sample", StringComparison.OrdinalIgnoreCase))
            {
                Generate_WebRequestSample(@"C:\Download", 5, 250000, 1000 * 1000, 3, (args.Length > 1 && args[1].Equals("all", StringComparison.OrdinalIgnoreCase)));
                return;
            }

            string xformProjectPath = Path.GetFullPath(Path.Combine(@"..\..\..\XForm"));

            string comparerPath = Path.Combine(xformProjectPath, @"Types\Comparers");
            Console.WriteLine($"Building Comparers to [{comparerPath}]...");
            ComparerGenerator.GenerateAll(comparerPath);

            string primitiveConverterPath = Path.Combine(xformProjectPath, @"Types\PrimitiveConverters.cs");
            Console.WriteLine($"Building [{primitiveConverterPath}]...");
            PrimitiveConvertersGenerator.Generate(primitiveConverterPath);

            Console.WriteLine("Done.");
        }

        private static void Generate_WebRequestSample(string basePath, int randomSeed, int userCount, int eventCount, int numberOfDays, bool includeBig)
        {
            Random r = new Random(randomSeed);
            DateTime asOfDate = DateTime.UtcNow.Date;
            String8Block block = new String8Block();
            WebRequestGenerator generator;

            string path;

            // Generate a set of users and write them out [for a week ago]
            asOfDate = asOfDate.AddDays(-8);
            path = Path.Combine(basePath, $"Users.{asOfDate:yyyyMMdd}.r{randomSeed}.{userCount}.csv");
            Console.WriteLine($"Writing {path}...");
            UserGenerator userGenerator = new UserGenerator(r, asOfDate);
            List<User> users = userGenerator.Next(userCount);
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
            for (int day = 0; day < numberOfDays; ++day)
            {
                generator = new WebRequestGenerator(users, r, asOfDate, (eventCount < 1001 ? 10 : 100));
                if (day == 0) generator.Issue = new PremiumUserOutage(asOfDate.AddMinutes(18), asOfDate.AddMinutes(104), r);
                BuildWebRequests(basePath, "WebRequest", generator, eventCount, WebRequestWriteMode.UserIdentityOnly);
                asOfDate = asOfDate.AddDays(-1);
            }

            // Generate one big joinable batch
            if (includeBig)
            {
                eventCount = 10 * 1000 * 1000;
                generator = new WebRequestGenerator(users, r, asOfDate, 1000);
                generator.Issue = new PortRangeBlocked(asOfDate.AddMinutes(1), asOfDate.AddMinutes(180), 11450, 11480);
                BuildWebRequests(basePath, "WebRequestBig", generator, eventCount, WebRequestWriteMode.UserIdentityOnly);
                asOfDate = asOfDate.AddDays(-1);

                // Generate one huge minimal batch
                eventCount = 100 * 1000 * 1000;
                generator = new WebRequestGenerator(users, r, asOfDate, 1000);
                generator.Issue = new UncachedSlowness(asOfDate.AddMinutes(4), asOfDate.AddMinutes(36), r);
                BuildWebRequests(basePath, "WebRequestHuge", generator, eventCount, WebRequestWriteMode.Minimal);
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
