// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class BinarySerializableExtensions
    {
        /// <summary>
        ///  Load each item in a path which might be a file containing one item
        ///  or a directory containing a set of items together.
        /// </summary>
        /// <typeparam name="T">IBinarySerializable type of item to load</typeparam>
        /// <param name="sourcePath">File Path of one item or Directory containing many</param>
        /// <param name="handleErrors">True to catch and log errors, false to let them propagate</param>
        /// <returns>Enumerable set of items successfully read</returns>
        public static IEnumerable<T> LoadEach<T>(string sourcePath, bool handleErrors = true) where T : IBinarySerializable, new()
        {
            if (Directory.Exists(sourcePath))
            {
                foreach (string filePath in Directory.GetFiles(sourcePath))
                {
                    T item = new T();

                    if (handleErrors)
                    {
                        if (item.TryFileRead(filePath, true)) yield return item;
                    }
                    else
                    {
                        item.FileRead(filePath);
                        yield return item;
                    }
                }
            }
            else if (File.Exists(sourcePath))
            {
                T item = new T();

                if (handleErrors)
                {
                    if (item.TryFileRead(sourcePath, true)) yield return item;
                }
                else
                {
                    item.FileRead(sourcePath);
                    yield return item;
                }
            }
            else
            {
                throw new FileNotFoundException(String.Format(Resources.LoadEach_FileNotFound, Path.GetFullPath(sourcePath)));
            }
        }

        /// <summary>
        ///  Load each item in a set of paths and return successfully loaded items.
        /// </summary>
        /// <typeparam name="T">IBinarySerializable type of item to load</typeparam>
        /// <param name="sourcePaths">File Path of one item or Directory containing many</param>
        /// <param name="handleErrors">True to catch and log errors, false to let them propagate</param>
        /// <returns>Enumerable set of items successfully read</returns>
        public static IEnumerable<T> LoadEach<T>(IEnumerable<string> sourcePaths, bool handleErrors = true) where T : IBinarySerializable, new()
        {
            foreach (string filePath in sourcePaths)
            {
                T item = new T();

                if (handleErrors)
                {
                    if (item.TryFileRead(filePath, true)) yield return item;
                }
                else
                {
                    item.FileRead(filePath);
                    yield return item;
                }
            }
        }

        #region Read and Write
        /// <summary>
        ///  Write this IBinarySerializable item in binary format to a file with the given path.
        ///  The file will be replaced if it exists already.
        /// </summary>
        /// <param name="item">Item to write</param>
        /// <param name="filePath">File Path to write binary form of item to</param>
        public static void FileWrite(this IBinarySerializable item, string filePath)
        {
            // Ensure the containing folder exists
            string serializationDirectory = Path.GetDirectoryName(filePath);
            if (!String.IsNullOrEmpty(serializationDirectory)) Directory.CreateDirectory(serializationDirectory);

            // Serialize the item
            long lengthWritten = 0;
            FileStream s = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Delete);
            using (BinaryWriter writer = new BinaryWriter(s))
            {
                item.WriteBinary(writer);
                lengthWritten = s.Position;
            }

            // If nothing was written, delete the file
            if (lengthWritten == 0) File.Delete(filePath);
        }

        /// <summary>
        ///  Try to read this IBinarySerializable item from the binary data at the given file path.
        ///  Catch any IOExceptions and return false if the item couldn't be fully read.
        ///  The state of the item after an exception is thrown is indeterminate.
        /// </summary>
        /// <param name="item">IBinarySerializable item to read</param>
        /// <param name="filePath">File Path to read binary form of item from</param>
        /// <param name="logErrors">True to log errors to the Trace log, False otherwise</param>
        /// <returns></returns>
        public static bool TryFileRead(this IBinarySerializable item, string filePath, bool logErrors = false)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                FileRead(item, filePath);
                return true;
            }
            catch (IOException ex)
            {
                if (logErrors) Trace.WriteLine(String.Format(Resources.TryFileRead_ExceptionDuringRead, filePath, ex.GetType().Name, ex.Message));
                return false;
            }
        }

        /// <summary>
        ///  Read this IBinarySerializable item in from the binary data at the given file path.
        ///  Throws exceptions encountered during reading (common if reading a file with the wrong class).
        /// </summary>
        /// <param name="item">IBinarySerializable item to read</param>
        /// <param name="filePath">File Path to read binary form of item from</param>
        public static void FileRead(this IBinarySerializable item, string filePath)
        {
            using (FileStream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                BinaryReader reader = new BinaryReader(s);
                item.ReadBinary(reader);
            }
        }
        #endregion

        #region Serialized File Properties
        public static long FileSize(this IBinarySerializable item, string filePath)
        {
            if (Directory.Exists(filePath))
            {
                long size = 0;
                foreach (string subfilePath in Directory.EnumerateFiles(filePath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    size += new FileInfo(subfilePath).Length;
                }

                return size;
            }
            else if (File.Exists(filePath))
            {
                return new FileInfo(filePath).Length;
            }
            else
            {
                return 0;
            }
        }

        public static bool FileExists(this IBinarySerializable item, string filePath)
        {
            return (File.Exists(filePath) || Directory.Exists(filePath));
        }

        public static void DeleteFile(this IBinarySerializable item, string filePath)
        {
            File.Delete(filePath);
        }
        #endregion
    }
}
