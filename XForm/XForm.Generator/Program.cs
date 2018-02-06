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
                Generate_WebRequestSample(5, 250000, 1000 * 1000, 3);
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

        private static void Generate_WebRequestSample(int randomSeed, int userCount, int eventCount, int numberOfDays)
        {
            Random r = new Random(randomSeed);
            DateTime asOfDate = DateTime.UtcNow.Date;
            String8Block block = new String8Block();
            WebRequestGenerator generator;

            string path;

            // Generate a set of users and write them out [for a week ago]
            asOfDate = asOfDate.AddDays(-8);
            path = $"Users.{asOfDate:yyyyMMdd}.r{randomSeed}.{userCount}.csv";
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
            BuildWebRequests(generator, eventCount, WebRequestWriteMode.All);

            asOfDate = asOfDate.AddDays(8);

            // Generate batches of WebRequest sample data [current]
            for (int day = 0; day < numberOfDays; ++day)
            {
                generator = new WebRequestGenerator(users, r, asOfDate, (eventCount < 1001 ? 10 : 100));
                if(day == 0) generator.Issue = new DataCenterOutage() { DataCenter = "Europe West", StartTime = asOfDate.AddSeconds(5), EndTime = asOfDate.AddSeconds(15) };
                BuildWebRequests(generator, eventCount, WebRequestWriteMode.All);
                asOfDate = asOfDate.AddDays(-1);
            }

            //// Generate one big joinable batch
            //eventCount = 10 * 1000 * 1000;
            //generator = new WebRequestGenerator(users, r, asOfDate, 1000);
            //generator.Issue = new PortRangeBlocked() { StartTime = asOfDate.AddMinutes(1), EndTime = asOfDate.AddMinutes(180), StartPort = 11450, EndPort = 11480 };
            //BuildWebRequests(generator, eventCount, WebRequestWriteMode.UserEmailOnly);
            //asOfDate = asOfDate.AddDays(-1);

            //// Generate one huge minimal batch
            //eventCount = 100 * 1000 * 1000;
            //generator = new WebRequestGenerator(users, r, asOfDate, 1000);
            //generator.Issue = new UncachedSlowness() { Random = r, StartTime = asOfDate.AddMinutes(4), EndTime = asOfDate.AddMinutes(36) };
            //BuildWebRequests(generator, eventCount, WebRequestWriteMode.Minimal);

            Console.WriteLine("Done.");
        }

        private static void BuildWebRequests(WebRequestGenerator generator, int eventCount, WebRequestWriteMode mode)
        {
            DateTime asOfDate = generator.Current;
            String8Block block = new String8Block();

            string path = $"WebRequestSample.{asOfDate:yyyyMMdd}.r5.{eventCount}.csv";
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
