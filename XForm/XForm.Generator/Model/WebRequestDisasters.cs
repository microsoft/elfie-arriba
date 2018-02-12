// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Generator.Model
{
    public interface IDisaster<T>
    {
        void Transform(T item);
    }

    public class DataCenterOutage : IDisaster<WebRequest>
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string DataCenter { get; set; }

        public DataCenterOutage(DateTime startTime, DateTime endTime, string dataCenter)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.DataCenter = dataCenter;
        }

        public void Transform(WebRequest item)
        {
            if (item.EventTime > EndTime) return;
            if (item.EventTime < StartTime) return;
            if (item.DataCenter != DataCenter) return;

            item.HttpStatus = 500;
        }
    }

    public class UncachedSlowness : IDisaster<WebRequest>
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Random Random { get; set; }

        public UncachedSlowness(DateTime startTime, DateTime endTime, Random r)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Random = r;
        }

        public void Transform(WebRequest item)
        {
            if (item.EventTime > EndTime) return;
            if (item.EventTime < StartTime) return;

            if (item.WasCachedResponse == false)
            {
                if ((Random.Next() & 7) < 4) item.TimeTakenMs = item.TimeTakenMs * 4;
            }
        }
    }

    public class PremiumUserOutage : IDisaster<WebRequest>
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Random Random { get; set; }

        public PremiumUserOutage(DateTime startTime, DateTime endTime, Random r)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Random = r;
        }

        public void Transform(WebRequest item)
        {
            if (item.EventTime > EndTime) return;
            if (item.EventTime < StartTime) return;

            if (item.User != null && item.User.IsPremiumUser)
            {
                if ((Random.Next() & 7) < 5) item.HttpStatus = 500;
            }
        }
    }

    public class PortRangeBlocked : IDisaster<WebRequest>
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ushort StartPort { get; set; }
        public ushort EndPort { get; set; }

        public PortRangeBlocked(DateTime startTime, DateTime endTime, ushort startPort, ushort endPort)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.StartPort = startPort;
            this.EndPort = endPort;
        }

        public void Transform(WebRequest item)
        {
            if (item.EventTime > EndTime) return;
            if (item.EventTime < StartTime) return;

            if (item.ServerPort >= StartPort && item.ServerPort <= EndPort)
            {
                item.HttpStatus = 404;
                item.ResponseBytes = 0;
            }
        }
    }
}
