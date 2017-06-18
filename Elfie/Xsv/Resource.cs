﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Xsv
{
    public static class Resource
    {
        /// <summary>
        ///  Read all lines from an embedded resource file.
        /// </summary>
        /// <param name="streamName">Stream Name (AssemblyName.Folders.FileName.ext)</param>
        /// <param name="asm">Assembly to read from, defaults to Xsv</param>
        /// <returns>Lines from file in stream</returns>
        public static string[] ReadAllStreamLines(string streamName, Assembly asm = null)
        {
            if (asm == null) asm = Assembly.GetExecutingAssembly();

            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(asm.GetManifestResourceStream(streamName)))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        ///  Save an embedded resource stream to a file.
        /// </summary>
        /// <param name="streamName">Stream Name (AssemblyName.Folders.FileName.ext)</param>
        /// <param name="filePath">Relative Path to save stream to</param>
        /// <param name="asm">Assembly to read from, defaults to Xsv</param>
        public static void SaveStreamTo(string streamName, string filePath, Assembly asm = null)
        {
            if (asm == null) asm = Assembly.GetExecutingAssembly();

            using (FileStream output = new FileStream(filePath, FileMode.Create))
            {
                using (Stream source = asm.GetManifestResourceStream(streamName))
                {
                    source.CopyTo(output);
                }
            }
        }
    }
}
