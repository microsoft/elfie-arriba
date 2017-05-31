// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Arriba.Structures
{
    /// <summary>
    ///  IpRange represents a single IPv4 address or an IPv4 address range.
    /// </summary>
    public class IpRange
    {
        private static Regex s_ipPartsExpression = new Regex(@"^(\d{1,3}|\.|-|\*|/)+$", RegexOptions.Compiled);
        public uint StartInclusive { get; set; }
        public uint EndInclusive { get; set; }

        public IpRange(uint start, uint end)
        {
            this.StartInclusive = start;
            this.EndInclusive = end;
        }

        /// <summary>
        ///  Parse an IP address or IP address range.
        ///   Accepts:
        ///     IP address        - "10.1.15.250"
        ///     IP prefix         - "10.1"
        ///     IP prefix w/ .*   - "10.1.*"
        ///     IP range          - "10.1.15.0-10.1.18.200"
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="result">IpRange result, if valid</param>
        /// <returns>True if valid, False otherwise</returns>
        public static bool TryParse(string value, out IpRange result)
        {
            result = new IpRange(0, 0);
            if (string.IsNullOrEmpty(value)) return true;

            // If the text doesn't contain IP address parts (digits, '.', '-'), return
            Match m = s_ipPartsExpression.Match(value);
            if (!m.Success) return false;

            // Get the first group (containing a capture for each 'token')
            Group g = m.Groups[1];
            int index = 0;

            uint startAddress, endAddress;

            // Try to parse one IP address
            int bitsFound = ParseIP(g, ref index, out startAddress);
            if (bitsFound == -1)
            {
                // Invalid address, stop
                return false;
            }
            else if (bitsFound == 32)
            {
                // Whole address and nothing left, return as a single IP
                if (index >= g.Captures.Count)
                {
                    result = new IpRange(startAddress, startAddress);
                    return true;
                }

                // '-', address range
                if (g.Captures[index].Value == "-")
                {
                    ++index;
                    bitsFound = ParseIP(g, ref index, out endAddress);

                    // If the second address was incomplete, stop
                    if (bitsFound < 32) return false;

                    // If the second address is smaller, stop
                    if (startAddress > endAddress) return false;

                    // Otherwise, we found a valid range
                    result = new IpRange(startAddress, endAddress);
                    return true;
                }

                // '/', prefix bit count
                if (g.Captures[index].Value == "/")
                {
                    ++index;

                    // Get the prefix bit count
                    if (!int.TryParse(g.Captures[index].Value, out bitsFound)) return false;
                    ++index;

                    // Clear the suffix bits on the sample address
                    startAddress = startAddress & (uint.MaxValue << (32 - bitsFound));

                    // Compute the end address
                    endAddress = startAddress + (uint)((1 << (32 - bitsFound)) - 1);

                    result = new IpRange(startAddress, endAddress);
                    return true;
                }

                // Otherwise, invalid
                return false;
            }
            else
            {
                // Partial IP address - make a range with the remaining unfound bits

                // If this isn't everything, stop
                if (index < g.Captures.Count) return false;

                // Otherwise, end address is start plus the remaining octets
                if (bitsFound == 0)
                {
                    endAddress = uint.MaxValue;
                }
                else
                {
                    endAddress = startAddress + (uint)((1 << (32 - bitsFound)) - 1);
                }

                result = new IpRange(startAddress, endAddress);
                return true;
            }
        }

        /// <summary>
        ///  Parse a single IP address from regex match parts.
        ///   This accepts:
        ///     Complete IPs (four numbers with dot separators),
        ///     IP prefixes (fewer than four parts),
        ///     IP prefix with '.*' suffix/
        /// </summary>
        /// <param name="g">Group containing captures for each number, '.', '*'</param>
        /// <param name="index">Next group index to check</param>
        /// <param name="address">IP address uint found</param>
        /// <returns>Number of bits defined in IP, -1 for invalid</returns>
        private static int ParseIP(Group g, ref int index, out uint address)
        {
            int nextMaskBits = 24;
            address = 0;

            while (index < g.Captures.Count)
            {
                Capture c = g.Captures[index];

                // If the next part is '*', stop successfully
                if (c.Value == "*")
                {
                    ++index;
                    break;
                }

                // The next part must be a number
                uint part;
                if (!uint.TryParse(c.Value, out part)) return -1;
                ++index;

                // Verify the part is in range [0, 255]
                if (part > 255) return -1;

                // Add it to the current address
                address += part << nextMaskBits;
                nextMaskBits -= 8;

                // If we've gotten four parts, stop
                if (nextMaskBits < 0) break;

                // The next part must be the end or a '.'
                if (index < g.Captures.Count && g.Captures[index].Value != ".") return -1;
                ++index;
            }

            return (24 - nextMaskBits);
        }

        private static void AddString(uint address, StringBuilder result)
        {
            result.Append((address >> 24) & 0XFF);
            result.Append(".");
            result.Append((address >> 16) & 0XFF);
            result.Append(".");
            result.Append((address >> 8) & 0XFF);
            result.Append(".");
            result.Append(address & 0XFF);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            AddString(this.StartInclusive, result);

            if (this.EndInclusive != this.StartInclusive)
            {
                result.Append("-");
                AddString(this.EndInclusive, result);
            }

            return result.ToString();
        }
    }
}
