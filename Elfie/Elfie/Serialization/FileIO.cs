// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public static class FileIO
    {
        /// <summary>
        ///  CleanPath removes illegal characters and trims a candidate file path
        ///  to a given length limit, if provided.
        /// </summary>
        /// <param name="filePath">Path to clean</param>
        /// <param name="lengthLimit">Length limit of desired path</param>
        /// <returns>File Path without any invalid characters and within the length limit</returns>
        public static string CleanPath(string filePath, int lengthLimit = -1)
        {
            if (String.IsNullOrEmpty(filePath)) return String.Empty;

            // Remove invalid characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filePath = filePath.Replace(c.ToString(), "");
            }

            // Trim to length limit if required
            if (lengthLimit > 0 || filePath.Length <= lengthLimit)
            {
                return filePath;
            }
            else
            {
                return filePath.Substring(0, lengthLimit);
            }
        }

        /// <summary>
        ///  IsManagedBinary returns whether the file extension of the file is one
        ///  used by managed binaries.
        /// </summary>
        /// <param name="filePath">File Path to file to identify</param>
        /// <returns>True if extension is one used by managed binaries, false otherwise</returns>
        public static bool IsManagedBinary(string filePath)
        {
            string lowercaseExtension = Path.GetExtension(filePath).ToLowerInvariant();
            return lowercaseExtension.Equals(".dll") || lowercaseExtension.Equals(".exe") || lowercaseExtension.Equals(".winmd");
        }
    }
}
