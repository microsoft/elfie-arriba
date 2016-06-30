// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Arriba.Serialization
{
    // TODO: Revisit write and replace concept.
    // TODO: Do we want to change exception model from "try to hide"?
    public class FileIO
    {
        public static bool Replace(string sourcePath, string endPath)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(120);
            Stopwatch w = Stopwatch.StartNew();

            // If the destination file exists, keep trying to delete it
            if (File.Exists(endPath))
            {
                while (w.Elapsed < timeout)
                {
                    try
                    {
                        File.Delete(endPath);
                        break;
                    }
                    catch (IOException)
                    {
                        // File in use or other error. Wait a bit and retry
                        Thread.Sleep(250);
                    }
                }
            }

            // Now, move our temporary over the new one
            if (File.Exists(sourcePath))
            {
                while (w.Elapsed < timeout)
                {
                    try
                    {
                        File.Move(sourcePath, endPath);
                        break;
                    }
                    catch (IOException)
                    {
                        // File in use or other error. Wait a bit and retry
                        Thread.Sleep(250);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
