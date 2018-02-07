// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace XForm.Generator.Model
{
    public enum WebRequestWriteMode
    {
        All,
        UserIdentityOnly,
        Minimal
    }

    public class WebRequest
    {
        public DateTime EventTime { get; set; }

        public bool IsAnonymous { get; set; }
        public User User { get; set; }

        public int ClientIP { get; set; }

        public string ServerName { get; set; }
        public ushort ServerPort { get; set; }
        public string HttpMethod { get; set; }
        public string UriStem { get; set; }

        public ushort HttpStatus { get; set; }
        public int? RequestBytes { get; set; }
        public int ResponseBytes { get; set; }
        public float TimeTakenMs { get; set; }
        public string Protocol { get; set; }
        public bool WasEncrypted { get; set; }
        public bool WasCachedResponse { get; set; }

        public string DataCenter { get; set; }

        public void WriteTo(ITabularWriter writer, String8Block block, int id, WebRequestWriteMode mode)
        {
            if (writer.RowCountWritten == 0)
            {
                List<string> columnNames = new List<string>(new string[] {
                    "ID",
                    "EventTime",
                    "DataCenter",
                    "ServerName",
                    "ServerPort",
                    "HttpMethod",
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
                });


                if (mode != WebRequestWriteMode.Minimal)
                {
                    columnNames.Add("ClientIP");
                    columnNames.Add("UriStem");
                    columnNames.Add("UserID");

                    if (mode != WebRequestWriteMode.UserIdentityOnly)
                    {
                        columnNames.AddRange(new string[] {
                            "UserEmailAddress",
                            "UserGuid",
                            "IsPremiumUser",
                            "JoinDate"
                        });
                    }
                }

                writer.SetColumns(columnNames);
            }

            block.Clear();

            writer.Write(id);
            writer.Write(this.EventTime);
            writer.Write(block.GetCopy(this.DataCenter));
            writer.Write(block.GetCopy(this.ServerName));
            writer.Write(this.ServerPort);
            writer.Write(block.GetCopy(this.HttpMethod));
            writer.Write(this.HttpStatus);
            if (this.RequestBytes.HasValue) { writer.Write(this.RequestBytes.Value); } else { writer.Write(String8.Empty); }
            writer.Write(this.ResponseBytes);
            writer.Write((int)this.TimeTakenMs);
            writer.Write(block.GetCopy(this.Protocol));
            writer.Write(this.WasEncrypted);
            writer.Write(this.WasCachedResponse);

            writer.Write(block.GetCopy(this.User.Region));
            writer.Write(block.GetCopy(this.User.Browser));
            writer.Write(block.GetCopy(this.User.OS));

            if (mode != WebRequestWriteMode.Minimal)
            {
                writer.Write(this.ClientIP);
                writer.Write(block.GetCopy(this.UriStem));

                if (this.IsAnonymous) { writer.Write(String8.Empty); } else { writer.Write(this.User.ID); }

                if (mode != WebRequestWriteMode.UserIdentityOnly)
                {
                    if (this.IsAnonymous) { writer.Write(String8.Empty); } else { writer.Write(block.GetCopy(this.User.EmailAddress)); }
                    if (this.IsAnonymous) { writer.Write(String8.Empty); } else { writer.Write(block.GetCopy(this.User.Guid.ToString())); }
                    if (this.IsAnonymous) { writer.Write(String8.Empty); } else { writer.Write(this.User.IsPremiumUser); }
                    if (this.IsAnonymous) { writer.Write(String8.Empty); } else { writer.Write(this.User.JoinDate); }
                }
            }

            writer.NextRow();
        }
    }
}
