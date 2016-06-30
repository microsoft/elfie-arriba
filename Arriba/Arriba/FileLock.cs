// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Arriba
{
    /// <summary>
    ///  FileLock is used to get cross-process synchronization locks using locked files.
    ///  They're automatically unlocked if the process holding them dies, and side-by-side
    ///  locks can be obtained by using different names or under different paths.
    ///  
    ///  Usage:
    ///  using(FileLock lock = FileLock.TryGet(name))
    ///  {
    ///     // TryGet returns null if unable to get lock; you need to check. Using blocks work for potentially null variables.
    ///     if(lock == null)
    ///     {
    ///         Console.WriteLine("Unable to get lock.");
    ///         return;
    ///     }
    ///     
    ///     // App Logic
    ///  }
    /// </summary>
    public class FileLock : IDisposable
    {
        private FileStream LockStream { get; set; }

        private FileLock(FileStream stream)
        {
            LockStream = stream;
        }

        /// <summary>
        ///  Try to get a FileLock for the given file. Only one application can get the lock,
        ///  it's unlocked if the app dies, and you can have side-by-side locks by using different
        ///  names or folders.
        /// </summary>
        /// <param name="fileName">Name of lock file to get</param>
        /// <returns>FileLock if lock gotten, null if not</returns>
        public static FileLock TryGet(string fileName)
        {
            string folderPath = Path.GetDirectoryName(fileName);
            if (!String.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            try
            {
                return new FileLock(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read));
            }
            catch (IOException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  Dispose releases the lock and deletes the underlying file.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (LockStream != null)
            {
                LockStream.Close();
                LockStream.Dispose();
                File.Delete(LockStream.Name);
                LockStream = null;
            }
        }
    }
}
