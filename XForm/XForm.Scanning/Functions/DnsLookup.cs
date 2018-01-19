// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    /// <summary>
    ///  DnsLookupBuilder adds a custom function, live DNS lookup, to XForm.
    /// </summary>
    internal class DnsLookupBuilder : IFunctionBuilder
    {
        public string Name => "DnsLookup";
        public string Usage => "DnsLookup({ComputerName})";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            // Strings in XForm are stored in the type 'String8'. 
            // Make a String8Block (like StringBuilder) to allocate space for the new strings for the resolved IP addresses.
            String8Block block = new String8Block();

            return SimpleTransformFunction<String8, String8>.Build(Name, source,
                context.Parser.NextColumn(source, context, typeof(String8)),
                (name8) =>
                {
                    // Convert the value passed for the name column to a string
                    string machineName = name8.ToString();

                    // Run the code to do the lookup
                    string firstIpAddress = LookupName(machineName);

                    // Convert back to XForm's String8 type
                    return block.GetCopy(firstIpAddress);
                },
                () =>
                {
                    // Before each page, clear the String8Block to reuse the memory
                    block.Clear();
                }
            );
        }

        private static string LookupName(string name)
        {
            try
            {
                // Do a DNS lookup of the machine
                IPAddress[] addresses = Dns.GetHostAddresses(name);

                // If it resolved, return the first IP address
                foreach (IPAddress address in addresses)
                {
                    return address.ToString();
                }
            }
            catch (SocketException)
            {
                // If it didn't resolve, return empty string
            }

            return "";
        }
    }
}
