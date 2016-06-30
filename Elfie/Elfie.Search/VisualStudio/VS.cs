// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace Microsoft.CodeAnalysis.Elfie.Search.VisualStudio
{
    public class LocationWithinFile
    {
        public string FilePath { get; set; }
        public int Line { get; set; }
        public int CharInLine { get; set; }
    }

    /// <summary>
    ///  From: http://blogs.msdn.com/b/kirillosenkov/archive/2011/08/10/how-to-get-dte-from-visual-studio-process-id.aspx
    /// </summary>
    internal class VS
    {
        private class NativeMethods
        {
            [DllImport("ole32.dll")]
            public static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
        }

        /// <summary>
        ///  Return the currently active file and cursor position within the file in
        ///  the preferred Visual Studio instance.
        /// </summary>
        /// <param name="databasePath">Elfie Database Path to help select correct VS instance</param>
        /// <returns>LocationWithinFile containing open file and cursor position</returns>
        public static LocationWithinFile GetCurrentLocation(string databasePath = null)
        {
            List<string> fullHintList = new List<string>();
            if (!String.IsNullOrEmpty(databasePath)) fullHintList.Add(databasePath);

            EnvDTE.DTE dte = GetPreferredDTE(fullHintList);

            // Ask VS to open the desired file
            MessageFilter.Register();

            try
            {
                // Show the Main Window
                EnvDTE.Document document = dte.ActiveDocument;
                if (document == null)
                {
                    Trace.WriteLine("No active document found.");
                    return null;
                }

                EnvDTE.TextSelection selection = document.Selection as EnvDTE.TextSelection;
                if (selection == null)
                {
                    Trace.WriteLine(String.Format("Unable to find cursor position in {0}", document.FullName));
                }

                return new LocationWithinFile() { FilePath = document.FullName, Line = selection.ActivePoint.Line, CharInLine = selection.ActivePoint.LineCharOffset };
            }
            finally
            {
                MessageFilter.Revoke();
            }
        }

        public static void OpenFileToLine(string filePath, int lineNumber, string databasePath = null)
        {
            List<string> fullHintList = new List<string>();
            fullHintList.Add(filePath);
            if (!String.IsNullOrEmpty(databasePath)) fullHintList.Add(databasePath);

            OpenFileToLine(GetPreferredDTE(fullHintList), filePath, lineNumber);
        }

        public static void OpenFileToLine(string filePath, int lineNumber, IEnumerable<string> hintPaths)
        {
            List<string> fullHintList = new List<string>();
            fullHintList.Add(filePath);
            fullHintList.AddRange(hintPaths);

            OpenFileToLine(GetPreferredDTE(fullHintList), filePath, lineNumber);
        }

        private static void OpenFileToLine(EnvDTE.DTE dte, string filePath, int lineNumber)
        {
            // Ask VS to open the desired file
            MessageFilter.Register();

            try
            {
                // Show the Main Window
                dte.MainWindow.Activate();

                // Open the desired file
                dte.ExecuteCommand("File.OpenFile", filePath);

                // Go to the desired line
                if (lineNumber > 0)
                {
                    dte.ExecuteCommand("Edit.GoTo", lineNumber.ToString());
                }
            }
            finally
            {
                MessageFilter.Revoke();
            }
        }

        private static EnvDTE.DTE GetPreferredDTE(IEnumerable<string> hintPaths = null)
        {
            List<EnvDTE.DTE> candidates = new List<EnvDTE.DTE>();

            // Find potential instances of Visual Studio to use
            foreach (Process candidate in Process.GetProcessesByName("devenv"))
            {
                EnvDTE.DTE dte = GetDTE(candidate.Id);

                // If DTE can't be retrieved, skip this one
                if (dte == null) continue;

                // If it's debugging, skip this one
                if (dte.Mode == EnvDTE.vsIDEMode.vsIDEModeDebug) continue;

                candidates.Add(dte);
            }

            // Try to prefer an instance containing one of the desired items
            // Search in hint path order, so higher priority files to match on can be listed first
            if (hintPaths != null)
            {
                foreach (string hintPath in hintPaths)
                {
                    foreach (EnvDTE.DTE dte in candidates)
                    {
                        if (Matches(dte, hintPath)) return dte;
                    }
                }
            }

            // Otherwise, return any found instance
            if (candidates.Count > 0) return candidates[0];

            // If no candidates were found, start a new instance
            return StartNewVisualStudio();
        }

        private static bool Matches(EnvDTE.DTE dte, string hintPath)
        {
            if (String.IsNullOrEmpty(hintPath)) return false;

            EnvDTE.Solution solution = dte.Solution;
            if (solution == null) return false;

            string solutionFolder = Path.GetFullPath(Path.GetDirectoryName(solution.FullName));
            if (hintPath.StartsWith(solutionFolder)) return true;

            EnvDTE.ProjectItem item = solution.FindProjectItem(hintPath);
            return item != null;
        }

        private static EnvDTE.DTE StartNewVisualStudio()
        {
            // If none were found, start a new instance
            Process p = Process.Start("devenv.exe");

            // Get the DTE
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            Stopwatch w = Stopwatch.StartNew();

            while (w.Elapsed < timeout)
            {
                EnvDTE.DTE dte = GetDTE(p.Id);
                if (dte != null) return dte;
                Thread.Sleep(500);
            }

            throw new InvalidOperationException(String.Format("Unable to start Visual Studio within timeout {0}", timeout));
        }

        private static EnvDTE.DTE GetDTE(int processId)
        {
            string progId = "!VisualStudio.DTE.14.0:" + processId.ToString();
            object runningObject = null;

            IBindCtx bindCtx = null;
            IRunningObjectTable rot = null;
            IEnumMoniker enumMonikers = null;

            try
            {
                Marshal.ThrowExceptionForHR(NativeMethods.CreateBindCtx(reserved: 0, ppbc: out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMonikers);

                IMoniker[] moniker = new IMoniker[1];
                IntPtr numberFetched = IntPtr.Zero;
                while (enumMonikers.Next(1, moniker, numberFetched) == 0)
                {
                    IMoniker runningObjectMoniker = moniker[0];

                    string name = null;

                    try
                    {
                        if (runningObjectMoniker != null)
                        {
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Do nothing, there is something in the ROT that we do not have access to.
                    }

                    if (!string.IsNullOrEmpty(name) && string.Equals(name, progId, StringComparison.Ordinal))
                    {
                        Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out runningObject));
                        break;
                    }
                }
            }
            finally
            {
                if (enumMonikers != null)
                {
                    Marshal.ReleaseComObject(enumMonikers);
                }

                if (rot != null)
                {
                    Marshal.ReleaseComObject(rot);
                }

                if (bindCtx != null)
                {
                    Marshal.ReleaseComObject(bindCtx);
                }
            }

            return (EnvDTE.DTE)runningObject;
        }
    }
}
