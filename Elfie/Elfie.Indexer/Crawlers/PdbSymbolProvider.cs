// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis.Elfie.PDB;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    public struct ILSequencePoint
    {
        public string Document;
        public int ILOffset;
        public int StartLine;
        public int StartCharInLine;
    }

    // For now, open PDB files using legacy desktop SymBinder
    internal class PdbSymbolProvider : IDisposable
    {
        [Guid("809c652e-7396-11d2-9771-00a0c9b4d50c")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IMetaDataDispenser
        {
            // We need to be able to call OpenScope, which is the 2nd vtable slot.
            // Thus we need this one placeholder here to occupy the first slot..
            void DefineScope_Placeholder();

            [PreserveSig]
            int OpenScope([In, MarshalAs(UnmanagedType.LPWStr)] String szScope, [In] Int32 dwOpenFlags, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out Object punk);

            // Don't need any other methods.
        }

        // Since we're just blindly passing this interface through managed code to the Symbinder, we don't care about actually
        // importing the specific methods.
        // This needs to be public so that we can call Marshal.GetComInterfaceForObject() on it to get the
        // underlying metadata pointer.
        [Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        public interface IMetadataImport
        {
            // Just need a single placeholder method so that it doesn't complain about an empty interface.
            void Placeholder();
        }

        private class NativeMethods
        {
            [DllImport("clr.dll")]
            public static extern int MetaDataGetDispenser([In] ref Guid rclsid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out Object ppv);

            [DllImport("ole32.dll")]
            public static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, Int32 dwClsContext, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        }

        private void ThrowExceptionForHR(int hr)
        {
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
        }

        private IMetaDataDispenser _metadataDispenser;
        private ISymUnmanagedBinder _symBinder;
        private ISymUnmanagedReader _symReader;

        private string _binaryFilePath;
        private string _pdbFilePath;
        private SourceFileMap _sourceFileMap;

        private PdbSymbolProvider(string binaryFilePath, string pdbFilePath = "")
        {
            _binaryFilePath = binaryFilePath;
            _pdbFilePath = pdbFilePath;
            if (String.IsNullOrEmpty(_pdbFilePath)) _pdbFilePath = Path.ChangeExtension(binaryFilePath, ".pdb");

            // Create a COM Metadata dispenser
            Guid dispenserClassID = new Guid(0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8); // CLSID_CorMetaDataDispenser
            Guid dispenserIID = new Guid(0x809c652e, 0x7396, 0x11d2, 0x97, 0x71, 0x00, 0xa0, 0xc9, 0xb4, 0xd5, 0x0c); // IID_IMetaDataDispenser
            object objDispenser;
            Marshal.ThrowExceptionForHR(NativeMethods.MetaDataGetDispenser(ref dispenserClassID, ref dispenserIID, out objDispenser));
            _metadataDispenser = (IMetaDataDispenser)objDispenser;

            // Create a symbol binder [?]
            Guid symBinderClassID = new Guid(0x0A29FF9E, 0x7F9C, 0x4437, 0x8B, 0x11, 0xF4, 0x24, 0x49, 0x1E, 0x39, 0x31); // CLSID_CorSymBinder
            Guid symBinderIID = new Guid(0xAA544d42, 0x28CB, 0x11d3, 0xbd, 0x22, 0x00, 0x00, 0xf8, 0x08, 0x49, 0xbd); // IID_ISymUnmanagedBinder
            object objBinder;
            if (NativeMethods.CoCreateInstance(ref symBinderClassID,
                                IntPtr.Zero, // pUnkOuter
                                1, // CLSCTX_INPROC_SERVER
                                ref symBinderIID,
                                out objBinder) < 0)
                throw new InvalidComObjectException("PdbSymbolProvider unable to construct an ISymUnmanagedBinder.");
            _symBinder = (ISymUnmanagedBinder)objBinder;

            // Create a symbol reader
            Guid importerIID = new Guid(0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44); // IID_IMetaDataImport

            // Open an metadata importer on the given filename. We'll end up passing this importer straight
            // through to the Binder.
            object objImporter;
            Marshal.ThrowExceptionForHR(_metadataDispenser.OpenScope(binaryFilePath, 0x00000010 /* read only */, ref importerIID, out objImporter));

            string pdbFolderPath = Path.GetDirectoryName(pdbFilePath);

            ISymUnmanagedReader reader;
            Marshal.ThrowExceptionForHR(_symBinder.GetReaderForFile(objImporter, binaryFilePath, pdbFolderPath, out reader));

            _symReader = reader;

            // Create a source file map [if we can find the map file]
            _sourceFileMap = SourceFileMap.Load(pdbFilePath);
        }

        public static PdbSymbolProvider TryBuildProvider(string binaryFilePath)
        {
            string pdbFilePath = Path.ChangeExtension(binaryFilePath, ".pdb");

            // If there's no PDB next to the binary, look in the symbol cache
            if (!File.Exists(pdbFilePath))
            {
                if (String.IsNullOrEmpty(SymbolCache.Path)) return null;

                RsDsSignature signature = Assembly.ReadRsDsSignature(binaryFilePath);
                if (signature == null)
                {
                    Trace.WriteLine(String.Format("Unable to read PDB signature from '{0}'", binaryFilePath));
                    return null;
                }

                string pdbFileName = Path.ChangeExtension(Path.GetFileName(binaryFilePath), ".pdb");
                pdbFilePath = Path.Combine(SymbolCache.Path, pdbFileName, signature.ToString(), pdbFileName);

                // If there was no PDB in the SymbolCache either, return no provider
                if (!File.Exists(pdbFilePath))
                {
                    return null;
                }
            }

            try
            {
                PdbSymbolProvider provider = new PdbSymbolProvider(binaryFilePath, pdbFilePath);
                return provider;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("Unable to build PDB provider for '{0}'. Error: {1}", binaryFilePath, ex.Message));
                return null;
            }
        }

        private Dictionary<ISymUnmanagedDocument, string> _urlCache = new Dictionary<ISymUnmanagedDocument, string>();

        private string GetUrl(ISymUnmanagedDocument doc)
        {
            string url;
            if (_urlCache.TryGetValue(doc, out url))
                return url;

            int urlLength;
            ThrowExceptionForHR(doc.GetUrl(0, out urlLength, null));

            // urlLength includes terminating '\0'
            char[] urlBuffer = new char[urlLength];
            ThrowExceptionForHR(doc.GetUrl(urlLength, out urlLength, urlBuffer));

            url = new string(urlBuffer, 0, urlLength - 1);

            // Map the URL, if a map is available
            if (_sourceFileMap != null) url = _sourceFileMap[url];

            _urlCache.Add(doc, url);
            return url;
        }

        public string CacheLocation(string url)
        {
            if (_sourceFileMap == null) return url;

            SourceFileDetails details = _sourceFileMap.Details(url);
            if (details == null) return url;

            return SourceFileMap.ComputeCachedPath(_pdbFilePath, details.SourceUrl);
        }

        public string FirstDocumentUrl()
        {
            ISymUnmanagedDocument[] docs = new ISymUnmanagedDocument[1];

            int count;
            ThrowExceptionForHR(_symReader.GetDocuments(docs.Length, out count, docs));

            if (count == 0) return String.Empty;
            return GetUrl(docs[0]);
        }

        public IEnumerable<string> AllDocumentUrls()
        {
            if (_symReader == null) return Array.Empty<string>();

            int arraySize = 100;
            int countFound = arraySize;

            // Read document list into bigger arrays until they fit (no API to get count or page through)
            ISymUnmanagedDocument[] docs = null;
            while (arraySize == countFound)
            {
                arraySize *= 10;
                docs = new ISymUnmanagedDocument[arraySize];
                ThrowExceptionForHR(_symReader.GetDocuments(docs.Length, out countFound, docs));
            }

            List<string> documentUrls = new List<string>();
            for (int i = 0; i < countFound; ++i)
            {
                documentUrls.Add(GetUrl(docs[i]));
            }
            return documentUrls;
        }

        public bool TryGetFirstPointForMethod(int methodToken, out ILSequencePoint point)
        {
            point = default(ILSequencePoint);

            if (_symReader == null) return false;

            ISymUnmanagedMethod symbolMethod;
            if (_symReader.GetMethod(methodToken, out symbolMethod) < 0) return false;

            int count = 1;
            ISymUnmanagedDocument[] docs = new ISymUnmanagedDocument[count];
            int[] startLines = new int[count];
            int[] startCharInLines = new int[count];
            int[] ilOffsets = new int[count];

            ThrowExceptionForHR(symbolMethod.GetSequencePoints(count, out count, ilOffsets, docs, startLines, startColumns: startCharInLines, endLines: null, endColumns: null));

            if (count == 0) return false;

            point = new ILSequencePoint() { Document = GetUrl(docs[0]), StartLine = startLines[0], StartCharInLine = startCharInLines[0], ILOffset = ilOffsets[0] };
            return true;
        }

        public IEnumerable<ILSequencePoint> GetSequencePointsForMethod(int methodToken)
        {
            if (_symReader == null)
                yield break;

            ISymUnmanagedMethod symbolMethod;
            if (_symReader.GetMethod(methodToken, out symbolMethod) < 0)
                yield break;

            int count;
            ThrowExceptionForHR(symbolMethod.GetSequencePointCount(out count));

            ISymUnmanagedDocument[] docs = new ISymUnmanagedDocument[count];
            int[] startLines = new int[count];
            int[] startCharInLines = new int[count];
            int[] ilOffsets = new int[count];

            ThrowExceptionForHR(symbolMethod.GetSequencePoints(count, out count, ilOffsets, docs, startLines, startColumns: startCharInLines, endLines: null, endColumns: null));

            for (int i = 0; i < count; i++)
            {
                if (startLines[i] == 0xFEEFEE)
                    continue;

                yield return new ILSequencePoint() { Document = GetUrl(docs[i]), StartLine = startLines[i], StartCharInLine = startCharInLines[i], ILOffset = ilOffsets[i] };
            }
        }

        public void Dispose()
        {
            if (_symReader != null)
            {
                Marshal.ReleaseComObject(_symReader);
                _symReader = null;
            }

            if (_symBinder != null)
            {
                Marshal.ReleaseComObject(_symBinder);
                _symBinder = null;
            }

            if (_metadataDispenser != null)
            {
                Marshal.ReleaseComObject(_metadataDispenser);
                _metadataDispenser = null;
            }
        }
    }
}

