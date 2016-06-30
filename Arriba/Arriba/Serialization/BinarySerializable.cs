// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Arriba.Serialization
{
    public static class BinarySerializable
    {
        #region CachePath
        private static readonly string[] s_emptyStringArray = new string[0];

        private static string s_cachePath;

        public static string CachePath
        {
            get
            {
                // If no CachePath is provided, it defaults to two levels above the application. If built to bin\Release, this
                // will then be a sibling of bin.
                if (s_cachePath == null)
                {
                    s_cachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\DiskCache");
                }

                return s_cachePath;
            }
            set
            {
                s_cachePath = value;
            }
        }
        #endregion

        public static string FullPath(params string[] path)
        {
            string filePath = Path.Combine(path);

            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            return Path.Combine(CachePath, filePath);
        }

        #region Read and Write
        public static void Write(this IBinarySerializable item, string filePath)
        {
            string fullPath = FullPath(filePath);

            // Ensure the containing folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Serialize (to a '.new' file)
            using (SerializationContext context = new SerializationContext(new FileStream(fullPath + ".new", FileMode.Create, FileAccess.Write, FileShare.Delete)))
            {
                item.WriteBinary(context);
            }

            // Overwrite the base file semi-atomically (if file exists, it's fully written)
            FileIO.Replace(fullPath + ".new", fullPath);
        }

        public static bool TryRead(this IBinarySerializable item, string filePath)
        {
            string fullPath = FullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                Read(item, filePath);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static void Read(this IBinarySerializable item, string filePath)
        {
            string fullPath = FullPath(filePath);

            using (SerializationContext context = new SerializationContext(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
            {
                item.ReadBinary(context);
            }
        }
        #endregion

        public static long Size(string filePath)
        {
            string fullPath = FullPath(filePath);
            if (Directory.Exists(fullPath))
            {
                long size = 0;
                foreach (string subfilePath in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    size += new FileInfo(subfilePath).Length;
                }

                return size;
            }
            else if (File.Exists(fullPath))
            {
                return new FileInfo(fullPath).Length;
            }
            else
            {
                return 0;
            }
        }

        public static bool Exists(string filePath)
        {
            string fullPath = FullPath(filePath);
            return (File.Exists(fullPath) || Directory.Exists(fullPath));
        }

        public static IEnumerable<string> EnumerateUnder(string filePath)
        {
            string pathToEnumerate = FullPath(filePath);
            if (Directory.Exists(pathToEnumerate))
                return Directory.EnumerateFiles(pathToEnumerate);
            else
                return s_emptyStringArray;
        }

        public static IEnumerable<string> EnumerateDirectoriesUnder(string filePath)
        {
            string pathToEnumerate = FullPath(filePath);
            if (Directory.Exists(pathToEnumerate))
                return Directory.EnumerateDirectories(pathToEnumerate);
            else
                return s_emptyStringArray;
        }

        public static void Delete(string filePath)
        {
            File.Delete(FullPath(filePath));
        }
    }
}
