// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Generator.Mappers;

namespace XForm.Generator.Model
{
    public class User
    {
        public int ID { get; set; }
        public string EmailAddress { get; set; }
        public Guid Guid { get; set; }
        public bool IsPremiumUser { get; set; }
        public DateTime JoinDate { get; set; }

        public string Region { get; set; }
        public string Browser { get; set; }
        public string OS { get; set; }

        public void WriteTo(ITabularWriter writer, String8Block block)
        {
            if (writer.RowCountWritten == 0)
            {
                writer.SetColumns(new string[] {
                    "UserID",
                    "UserGuid",
                    "EmailAddress",
                    "Region",
                    "Browser",
                    "OS",
                    "IsPremiumUser",
                    "JoinDate"
                });
            }

            block.Clear();

            writer.Write(this.ID);
            writer.Write(block.GetCopy(this.Guid.ToString()));
            writer.Write(block.GetCopy(this.EmailAddress));
            writer.Write(block.GetCopy(this.Region));
            writer.Write(block.GetCopy(this.Browser));
            writer.Write(block.GetCopy(this.OS));
            writer.Write(this.IsPremiumUser);
            writer.Write(this.JoinDate);

            writer.NextRow();
        }
    }

    public class UserGenerator
    {
        private Random Random { get; set; }
        private DateTime AsOfDate { get; set; }

        private GuidMapper GuidMapper { get; set; }
        private EmailAddressMapper EmailAddressMapper { get; set; }

        private FrequencyMapper<string> ClientRegionMapper { get; set; }
        private FrequencyMapper<string> ClientBrowserMapper { get; set; }
        private FrequencyMapper<string> ClientOSMapper { get; set; }

        public UserGenerator(Random r, DateTime asOfDate)
        {
            this.Random = r;
            this.AsOfDate = asOfDate;

            this.GuidMapper = new GuidMapper();
            this.EmailAddressMapper = new EmailAddressMapper();

            // ClientRegion: ISO 3166: US 28 % | CN 22 % | JP 17 % | GB 9 % | CA 8 % | IN 7 % | DE 5 % | AU 4 %
            this.ClientRegionMapper = new FrequencyMapper<string>(
                new string[] { "US", "CN", "JP", "GB", "CA", "IN", "DE", "AU" },
                new int[] { 28, 22, 17, 9, 8, 7, 5, 4 });

            // ClientBrowser: Chrome 58 30 % | IE 11 14 % | Chrome 57 10 % | Firefox 53 7 % | Chrome 45 5 % | Edge 14 5 % | Chrome 49 3 % | Chrome 56 2 % | Safari 10 2 % | IE 8 1.5 %
            this.ClientBrowserMapper = new FrequencyMapper<string>(
                new string[] { "Chrome 58", "IE 11", "Chrome 57", "Firefox 53", "Chrome 45", "Edge 14", "Chrome 49", "Chrome 56", "Safari 10", "IE 8" },
                new int[] { 300, 140, 100, 70, 50, 50, 30, 20, 20, 15 });

            // ClientOS: Windows 7 50 % | Windows 10 27 % | Windows 8.1 7 % | Windows XP 6 % | Mac OS X 10.12 4 % | Linux 2 % | Windows 8 1.6 % | Mac OS X 10.11 1.3 % | Mac OS X 10.10 0.9 %
            this.ClientOSMapper = new FrequencyMapper<string>(
                new string[] { "Windows 7", "Windows 10", "Windows 8.1", "Windows XP", "Mac OS X 10.12", "Linux", "Windows 8", "Mac OS X 10.11", "Mac OS X 10.10" },
                new int[] { 500, 270, 70, 60, 40, 20, 16, 13, 9 });
        }


        public User Next()
        {
            User user = new User();
            uint userIdentity = (uint)this.Random.Next();

            user.ID = unchecked((int)userIdentity);

            // UserName is an alias (for now)
            user.EmailAddress = this.EmailAddressMapper.Generate(userIdentity);

            // ClientRegion: ISO 3166: US 28 % | CN 22 % | JP 17 % | GB 9 % | CA 8 % | IN 7 % | DE 5 % | AU 4 %
            user.Region = this.ClientRegionMapper.Generate((uint)this.Random.Next());

            // ClientBrowser: Chrome 58 30 % | IE 11 14 % | Chrome 57 10 % | Firefox 53 7 % | Chrome 45 5 % | Edge 14 5 % | Chrome 49 3 % | Chrome 56 2 % | Safari 10 2 % | IE 8 1.5 %
            user.Browser = this.ClientBrowserMapper.Generate((uint)this.Random.Next());

            // ClientOS: Windows 7 50 % | Windows 10 27 % | Windows 8.1 7 % | Windows XP 6 % | Mac OS X 10.12 4 % | Linux 2 % | Windows 8 1.6 % | Mac OS X 10.11 1.3 % | Mac OS X 10.10 0.9 %
            user.OS = this.ClientOSMapper.Generate((uint)this.Random.Next());

            // Guid: Random
            user.Guid = this.GuidMapper.GenerateGuid((uint)userIdentity);

            // IsPremiumUser: 25% true
            user.IsPremiumUser = ((userIdentity % 4) == 0);

            // JoinDate: From now to 400 days ago
            user.JoinDate = this.AsOfDate.AddDays(-((userIdentity % 8000) / 20));

            return user;
        }

        public List<User> Next(int count)
        {
            List<User> set = new List<User>(count);

            for (int i = 0; i < count; ++i)
            {
                set.Add(this.Next());
            }

            return set;
        }
    }
}
