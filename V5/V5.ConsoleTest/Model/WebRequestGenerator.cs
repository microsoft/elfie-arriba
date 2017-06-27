using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Collections.Generic;
using V5.ConsoleTest.Extensions;
using V5.ConsoleTest.Generators;
using Xsv.Sanitize;

namespace V5.ConsoleTest.Model
{
    public class WebRequestGenerator
    {
        public Random Random { get; set; }
        public int CountPerSecond { get; set; }
        public DateTime Current { get; set; }
        private long TicksBetweenEvents { get; set; }

        private GuidMapper GuidMapper { get; set; }
        private EmailAddressMapper UserMapper { get; set; }
        private UriStemMapper UriStemMapper { get; set; }
        private DataCenterMapper DataCenterMapper { get; set; }

        private FrequencyMapper<string> HttpMethodMapper { get; set; }
        private FrequencyMapper<ushort> HttpStatusMapper { get; set; }
        private FrequencyMapper<ushort> ServerPortMapper { get; set; }
        private FrequencyMapper<string> ClientRegionMapper { get; set; }
        private FrequencyMapper<string> ClientBrowserMapper { get; set; }
        private FrequencyMapper<string> ClientOSMapper { get; set; }

        public WebRequestGenerator(Random r, DateTime start, int countPerSecond)
        {
            this.Random = r;
            this.Current = start.ToUniversalTime();
            this.CountPerSecond = countPerSecond;
            this.TicksBetweenEvents = TimeSpan.TicksPerSecond / this.CountPerSecond;

            this.GuidMapper = new GuidMapper();
            this.UserMapper = new EmailAddressMapper();
            this.UriStemMapper = new UriStemMapper();
            this.DataCenterMapper = new DataCenterMapper();

            // HttpMethod: GET 90% | POST 7% | DELETE 2% | PUT 1%
            this.HttpMethodMapper = new FrequencyMapper<string>(
                new string[] { "GET", "POST", "DELETE", "PUT" },
                new int[] { 90, 7, 2, 1 });

            // HttpStatus: 200 80% | 304 10% | 404 5% | 401 5% |  500 during issues 
            this.HttpStatusMapper = new FrequencyMapper<ushort>(
                new ushort[] { 200, 304, 404, 401 },
                new int[] { 80, 10, 5, 5 });

            // ServerPort: 443 40% | 80 40% | 11400 - 11600 20%
            this.ServerPortMapper = new FrequencyMapper<ushort>(
                new ushort[] { 443, 80, 11400 },
                new int[] { 40, 40, 20 });

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

        public WebRequest Next()
        {
            this.Current = this.Current.AddTicks(this.TicksBetweenEvents);

            WebRequest request = new WebRequest();
            bool isAnonymous = (this.Random.Next() % 16) < 4;
            uint userIdentity = (uint)this.Random.Next();

            // EventTime increases across events but not perfectly consistently
            request.EventTime = this.Current.AddMilliseconds((this.Random.Next() % 16) - 8);

            // ClientIP is completely random
            request.ClientIP = this.Random.Next();

            // UserName is an alias (for now)
            request.UserName = (isAnonymous ? null : this.UserMapper.Generate(userIdentity));

            // ServerName has three different stems and a numeric suffix for 96 options
            request.ServerName = ServerNameMapper.Generate((uint)this.Random.Next());

            // UriStem has 10 top values 90% of the time and a total of 512 + 256 + 10 = 778 unique values.
            request.UriStem = this.UriStemMapper.Generate((uint)this.Random.Next());

            // HttpMethod: GET 90% | POST 7% | DELETE 2% | PUT 1%
            request.HttpMethod = this.HttpMethodMapper.Generate((uint)this.Random.Next());

            // HttpStatus: 200 80% | 304 10% | 404 5% | 401 5% |  500 during issues 
            request.HttpStatus = this.HttpStatusMapper.Generate((uint)this.Random.Next());

            // RequestBytes is null except for POST, when it's random with a center at 1KB
            request.RequestBytes = (request.HttpMethod != "POST" ? (int?)null : this.Random.NormalDistribution(1024, 150));

            // ResponseBytes is normally distributed around 1KB
            request.ResponseBytes = this.Random.NormalDistribution(1024, 100);

            // TimeTaken is distributed around 100ms
            request.TimeTakenMs = this.Random.NormalDistribution(100, 12);

            // Protocol: All "TCP"
            request.Protocol = "TCP";

            // ServerPort: 443 40% | 80 40% | 11400 - 11600 20%
            request.ServerPort = this.ServerPortMapper.Generate((uint)this.Random.Next());
            if (request.ServerPort == 11400) request.ServerPort += (ushort)this.Random.Next(201);

            // WasEncrypted: [Port 443 or 50% of 11k port range]
            if (request.ServerPort == 443)
            {
                request.WasEncrypted = true;
            }
            else if (request.ServerPort > 11000)
            {
                request.WasEncrypted = this.Random.Next(2) == 1;
            }

            // WasCachedResponse: 80% true
            request.WasCachedResponse = (this.Random.Next(100) < 80);

            // ClientRegion: ISO 3166: US 28 % | CN 22 % | JP 17 % | GB 9 % | CA 8 % | IN 7 % | DE 5 % | AU 4 %
            request.ClientRegion = this.ClientRegionMapper.Generate((uint)this.Random.Next());

            // ClientBrowser: Chrome 58 30 % | IE 11 14 % | Chrome 57 10 % | Firefox 53 7 % | Chrome 45 5 % | Edge 14 5 % | Chrome 49 3 % | Chrome 56 2 % | Safari 10 2 % | IE 8 1.5 %
            request.ClientBrowser = this.ClientBrowserMapper.Generate((uint)this.Random.Next());

            // ClientOS: Windows 7 50 % | Windows 10 27 % | Windows 8.1 7 % | Windows XP 6 % | Mac OS X 10.12 4 % | Linux 2 % | Windows 8 1.6 % | Mac OS X 10.11 1.3 % | Mac OS X 10.10 0.9 %
            request.ClientOs = this.ClientOSMapper.Generate((uint)this.Random.Next());

            // DataCenter: West US 2 | Central US | East US 2 | West Europe | Central India | China East | Australia East
            request.DataCenter = this.DataCenterMapper.Generate(request.ClientRegion, (uint)this.Random.Next());

            if (!isAnonymous)
            {
                request.UserGuid = this.GuidMapper.GenerateGuid((uint)userIdentity);
                request.IsPremiumUser = ((userIdentity % 4) == 0);
                request.DaysSinceJoined = (ushort)((userIdentity % 8000) / 20);
            }

            return request;
        }

        public List<WebRequest> Next(int count)
        {
            List<WebRequest> set = new List<WebRequest>(count);

            for (int i = 0; i < count; ++i)
            {
                set.Add(this.Next());
            }

            return set;
        }

        public void WriteTo(ITabularWriter writer, int count)
        {
            String8Block block = new String8Block();

            writer.SetColumns(new string[] {
                "ID",
                "EventTime",
                "ClientIP",
                "UserName",
                "ServerName",
                "ServerPort",
                "HttpMethod",
                "UriStem",
                /* "UriQuery", */
                "HttpStatus",
                "RequestBytes",
                "ResponseBytes",
                "TimeTakenMs",
                "Protocol",
                "WasEncrypted",
                "WasCachedResponse",
                "ClientRegion",
                "ClientBrowser",
                "ClientOs",
                "DataCenter",
                "UserGuid",
                "IsPremiumUser",
                "DaysSinceJoined"
                /*, "ErrorStack" */ });

            for(int i = 0; i < count; ++i)
            {
                writer.Write(i);

                WebRequest request = this.Next();
                writer.Write(request.EventTime);
                writer.Write(request.ClientIP);
                writer.Write(block.GetCopy(request.UserName));
                writer.Write(block.GetCopy(request.ServerName));
                writer.Write(request.ServerPort);
                writer.Write(block.GetCopy(request.HttpMethod));
                writer.Write(block.GetCopy(request.UriStem));
                //writer.Write(block.GetCopy(request.UriQuery));
                writer.Write(request.HttpStatus);
                writer.Write(request.RequestBytes ?? 0);
                writer.Write(request.ResponseBytes);
                writer.Write((int)request.TimeTakenMs);
                writer.Write(block.GetCopy(request.Protocol));
                writer.Write(request.WasEncrypted);
                writer.Write(request.WasCachedResponse);
                writer.Write(block.GetCopy(request.ClientRegion));
                writer.Write(block.GetCopy(request.ClientBrowser));
                writer.Write(block.GetCopy(request.ClientOs));
                writer.Write(block.GetCopy(request.DataCenter));
                writer.Write(block.GetCopy(request.UserGuid.ToString()));
                writer.Write(request.IsPremiumUser ?? false);
                writer.Write(request.DaysSinceJoined ?? 0);
                //writer.Write(request.ErrorStack);

                writer.NextRow();
                block.Clear();
            }
        }
    }
}
