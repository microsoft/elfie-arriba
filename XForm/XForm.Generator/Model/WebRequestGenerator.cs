// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Generator.Extensions;
using XForm.Generator.Mappers;

namespace XForm.Generator.Model
{
    public class WebRequestGenerator
    {
        public Random Random { get; set; }
        public int CountPerSecond { get; set; }
        public DateTime Current { get; set; }

        public IDisaster<WebRequest> Issue { get; set; }

        private long TicksBetweenEvents { get; set; }

        private UriStemMapper UriStemMapper { get; set; }
        private DataCenterMapper DataCenterMapper { get; set; }

        private FrequencyMapper<string> HttpMethodMapper { get; set; }
        private FrequencyMapper<ushort> HttpStatusMapper { get; set; }
        private FrequencyMapper<ushort> ServerPortMapper { get; set; }

        private List<User> Users { get; set; }

        public WebRequestGenerator(List<User> users, Random r, DateTime start, int countPerSecond)
        {
            this.Users = users;

            this.Random = r;
            this.Current = start.ToUniversalTime();
            this.CountPerSecond = countPerSecond;
            this.TicksBetweenEvents = TimeSpan.TicksPerSecond / this.CountPerSecond;

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
        }

        public WebRequest Next()
        {
            this.Current = this.Current.AddTicks(this.TicksBetweenEvents);

            WebRequest request = new WebRequest();
            request.IsAnonymous = (this.Random.Next() % 16) < 4;
            request.User = Users[this.Random.Next() % Users.Count];

            // EventTime increases across events but not perfectly consistently
            request.EventTime = this.Current.AddMilliseconds((this.Random.Next() % 16) - 8);

            // ClientIP is completely random
            request.ClientIP = this.Random.Next();

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

            // DataCenter: West US 2 | Central US | East US 2 | West Europe | Central India | China East | Australia East
            request.DataCenter = this.DataCenterMapper.Generate(request.User.Region, (uint)this.Random.Next());

            if (this.Issue != null) this.Issue.Transform(request);

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
    }
}
