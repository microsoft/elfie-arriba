// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Index;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    /// <summary>
    ///  PackageDatabase is a database of the declared symbols for one or more
    ///  binaries which are part of a package. A package can be a NuGet package,
    ///  or a local solution or project.
    /// </summary>
    public class PackageDatabase : IStatistics, IBinarySerializable, IMemberDatabase
    {
        // NOTE: Update when the binary format changes so old code doesn't try to load newer content and vice-versa
        public static readonly int BinaryFileFormatVersion = 1;

        public PackageIdentity Identity;

        /// <summary>
        ///  The StringStore efficiently contains a single copy of each unique
        ///  string referenced within the PackageDatabase.
        /// </summary>
        public StringStore StringStore { get; internal set; }

        /// <summary>
        ///  The declared member tree contains all namespaces, types, and members
        ///  defined within the assembly.
        /// </summary>
        public ItemTree DeclaredMembers { get; internal set; }

        /// <summary>
        ///  The DeclaredMemberDetails array mirrors the declared member tree and
        ///  contains member types, modifiers, and signatures.
        /// </summary>
        public IList<SymbolDetails> DeclaredMemberDetails { get; internal set; }

        /// <summary>
        ///  The DeclaredMemberLocations array mirrors the declared member tree and
        ///  contains the file and location where the symbol at the same index is
        ///  defined.
        /// </summary>
        public IList<SymbolLocation> DeclaredMemberLocations { get; internal set; }

        /// <summary>
        ///  The File Tree contains the file hierarchy of all files containing
        ///  the codebase. Names in the tree do not contain path separators.
        /// </summary>
        public ItemTree FileTree { get; internal set; }

        /// <summary>
        ///  The Index contains a map from strings to members in the
        ///  DeclaredMembers tree.
        /// </summary>
        public MemberIndex Index { get; internal set; }

        public PackageDatabase() : this(new PackageIdentity(String.Empty))
        {
            // Serialization Constructor
        }

        public PackageDatabase(PackageIdentity identity)
        {
            this.Identity = identity;
            this.StringStore = new StringStore();
            this.FileTree = new ItemTree();

            // Build the symbol tree with a sentinel root
            this.DeclaredMembers = new ItemTree();

            // Populate sentinel details and location
            this.DeclaredMemberDetails = new List<SymbolDetails>();
            this.DeclaredMemberDetails.Add(new SymbolDetails() { Type = SymbolType.Any });
            this.DeclaredMemberLocations = new List<SymbolLocation>();
            this.DeclaredMemberLocations.Add(new SymbolLocation());

            this.Index = new MemberIndex();
        }

        public Symbol QueryRoot
        {
            get
            {
                return new Symbol(this, 0);
            }
        }

        public MutableSymbol MutableRoot
        {
            get
            {
                return new MutableSymbol(this, 0);
            }
        }

        #region Statistics
        public int TypeCount
        {
            get
            {
                int count = 0;
                this.QueryRoot.Walk((s) => { if (s.Type.IsType()) count++; });
                return count;
            }
        }

        public int MemberCount
        {
            get
            {
                int count = 0;
                this.QueryRoot.Walk((s) =>
                {
                    if (!s.Type.IsAboveType()) count++;
                });
                return count;
            }
        }

        public int LocatedMemberCount
        {
            get
            {
                int count = 0;
                this.QueryRoot.Walk((s) =>
                {
                    if (!s.Type.IsAboveType() && s.HasLocation) count++;
                });
                return count;
            }
        }

        public IStatistics StringStatistics
        {
            get { return StringStore; }
        }

        public IStatistics SymbolStatistics
        {
            get { return this.DeclaredMembers; }
        }

        public IStatistics FileStatistics
        {
            get { return this.FileTree; }
        }

        public IStatistics IndexStatistics
        {
            get { return this.Index; }
        }

        public int Count
        {
            get { return this.DeclaredMembers.Count; }
        }

        public long Bytes
        {
            get
            {
                // Approximate. Doesn't include package level details.
                return
                    this.StringStore.Bytes +
                    this.DeclaredMembers.Bytes +
                    this.FileTree.Bytes +
                    this.Index.Bytes +
                    this.DeclaredMemberDetails.Count * Marshal.SizeOf(typeof(SymbolDetails)) +
                    this.DeclaredMemberLocations.Count * Marshal.SizeOf(typeof(SymbolLocation));
            }
        }

        public bool IsEmpty
        {
            get { return this.Count <= 1; }
        }
        #endregion

        #region IMemberDatabase
        public String8 GetPackageName(int memberIndex)
        {
            // Return the package name from the PackageIdentity
            return String8.Convert(this.Identity.PackageName, new byte[String8.GetLength(this.Identity.PackageName)]);
        }

        public String8 GetPackageVersion(int memberIndex)
        {
            int ancestor;

            if (this.TryGetAncestorOfType(memberIndex, SymbolType.Package, out ancestor))
            {
                if (this.TryGetFirstChildOfType(memberIndex, SymbolType.Version, out ancestor))
                {
                    return this.StringStore[this.DeclaredMembers.GetNameIdentifier(ancestor)];
                }
            }
            return String8.Empty;
        }

        public int GetPopularityRank(int memberIndex)
        {
            // Return the package name from the PackageIdentity
            return (int)Math.Log(Math.Max(this.Identity.DownloadCount, 1), 2);
        }

        public String8 GetAssemblyName(int index)
        {
            int ancestor;

            if (this.TryGetAncestorOfType(index, SymbolType.Assembly, out ancestor))
            {
                return this.StringStore[this.DeclaredMembers.GetNameIdentifier(ancestor)];
            }
            return String8.Empty;
        }

        public String8 GetFrameworkTargets(int index)
        {
            int ancestor;

            if (this.TryGetAncestorOfType(index, SymbolType.FrameworkTarget, out ancestor))
            {
                return this.StringStore[this.DeclaredMembers.GetNameIdentifier(ancestor)];
            }
            return String8.Empty;
        }

        public SymbolType GetMemberType(int memberIndex)
        {
            return this.DeclaredMemberDetails[memberIndex].Type;
        }

        public SymbolModifier GetMemberModifiers(int memberIndex)
        {
            return this.DeclaredMemberDetails[memberIndex].Modifiers;
        }

        public String8 GetMemberParameters(int memberIndex)
        {
            return this.StringStore[this.DeclaredMemberDetails[memberIndex].ParametersIdentifier];
        }

        public Path8 GetMemberFilePath(int memberIndex)
        {
            int fileIndex = this.DeclaredMemberLocations[memberIndex].FileIndex;
            if (fileIndex <= 0) return Path8.Empty;

            // TODO: Handle '/' or '\' in ItemTree to avoid canonicalizing
            int pathRootIndex = this.FileTree.GetAncestorAtDepth(fileIndex, 1);
            String8 rootName = this.StringStore[this.FileTree.GetNameIdentifier(pathRootIndex)];
            bool isUrl = rootName.CompareTo("http:", true) == 0 || rootName.CompareTo("https:", true) == 0;
            char delimiter = isUrl ? '/' : '\\';

            return new Path8(this.StringStore, this.FileTree, fileIndex, delimiter);
        }

        public ushort GetMemberFileLine(int memberIndex)
        {
            return this.DeclaredMemberLocations[memberIndex].Line;
        }

        public ushort GetMemberCharInLine(int memberIndex)
        {
            return this.DeclaredMemberLocations[memberIndex].CharInLine;
        }

        public void SetLocation(int memberIndex, string filePath, ushort line, ushort charInLine)
        {
            // TODO: Handle '/' or '\' in ItemTree to avoid canonicalizing
            if (!String.IsNullOrEmpty(filePath) && (filePath.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || filePath.StartsWith("https:", StringComparison.OrdinalIgnoreCase)))
            {
                filePath = filePath.Replace('/', '\\');
            }

            // Find (or add) the file path to the File Tree
            int fileIndex = 0;
            if (!String.IsNullOrEmpty(filePath))
            {
                String8 path8 = String8.Convert(filePath, new byte[String8.GetLength(filePath)]);
                String8Set splitPath8 = path8.Split('\\', new int[String8Set.GetLength(path8, '\\')]);
                fileIndex = this.FileTree.AddPath(0, splitPath8, this.StringStore);
            }

            // Write the updated location
            SymbolLocation location;
            location.FileIndex = fileIndex;
            location.Line = line;
            location.CharInLine = charInLine;

            if (this.DeclaredMemberLocations.Count == memberIndex)
            {
                this.DeclaredMemberLocations.Add(location);
            }
            else if (this.DeclaredMemberLocations.Count > memberIndex)
            {
                this.DeclaredMemberLocations[memberIndex] = location;
            }
            else
            {
                throw new InvalidOperationException(String.Format(Resources.DatabaseArraysOutOfSync, "DeclaredMemberLocations"));
            }
        }
        #endregion

        #region Serialization
        public void ConvertToImmutable()
        {
            // Convert strings (and get remap array)
            this.StringStore.ConvertToImmutable();

            // Remap strings in DefinedSymbolTree
            if (DeclaredMembers.RequiresRemap())
            {
                DeclaredMembers.UpdateIdentifiers(StringStore);

                // Remap strings in DefinedSymbolDetails
                for (int i = 0; i < DeclaredMemberDetails.Count; ++i)
                {
                    SymbolDetails details = DeclaredMemberDetails[i];
                    details.UpdateIdentifiers(StringStore);
                    DeclaredMemberDetails[i] = details;
                }

                // Sort the DefinedSymbolTree by name and then parameters [same order as StringStore]
                DeclaredMembers.Sort((left, right) =>
                {
                    int nameCompare = DeclaredMembers.GetNameIdentifier(left).CompareTo(DeclaredMembers.GetNameIdentifier(right));
                    if (nameCompare != 0) return nameCompare;
                    return DeclaredMemberDetails[left].ParametersIdentifier.CompareTo(DeclaredMemberDetails[right].ParametersIdentifier);
                });
            }

            // Remap strings in FileTree
            if (FileTree.RequiresRemap()) FileTree.UpdateIdentifiers(StringStore);

            // Convert MemberIndex
            Index.ConvertToImmutable(StringStore);
        }

        public void WriteBinary(BinaryWriter w)
        {
            // Don't write database if empty (BinarySerializable will detect and delete file)
            if (IsEmpty) return;

            // Ensure the database has been converted to queryable/writable form
            ConvertToImmutable();

            w.Write(BinaryFileFormatVersion);

            // Write strings
            StringStore.WriteBinary(w);

            // Write symbol tree
            DeclaredMembers.WriteBinary(w);

            // Write details
            w.Write(DeclaredMemberDetails);

            // Write symbol locations
            w.Write(DeclaredMemberLocations);

            // Write file tree
            FileTree.WriteBinary(w);

            // Write search index
            Index.WriteBinary(w);

            // Write identity details (last; likely to change)
            // PackageIdentity must be last so DownloadCount can be seeked to
            Identity.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            int formatVersion = r.ReadInt32();
            if (formatVersion != BinaryFileFormatVersion)
            {
                throw new IOException(String.Format(Resources.DatabaseFormatVersionWrong, BinaryFileFormatVersion, formatVersion));
            }

            StringStore.ReadBinary(r);
            DeclaredMembers.ReadBinary(r);
            DeclaredMemberDetails = r.ReadList<SymbolDetails>();
            DeclaredMemberLocations = r.ReadList<SymbolLocation>();
            FileTree.ReadBinary(r);
            Index.ReadBinary(r);

            // PackageIdentity must be last so DownloadCount can be seeked to
            Identity.ReadBinary(r);
        }

        public static int ReadDownloadsOnly(BinaryReader r)
        {
            // PackageIdentity is last and DownloadCount is last within it, so the very last bytes are the download count
            r.BaseStream.Seek(-4, SeekOrigin.End);
            return r.ReadInt32();
        }

        public void WriteStringsOnly(string filePath)
        {
            StringStore.FileWrite(filePath);
        }
        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();
            // 0 == sentinel root
            WriteTree(sb, memberIndex: 0, indent: 0);
            return sb.ToString();
        }

        private void WriteTree(StringBuilder sb, int memberIndex, int indent)
        {
            SymbolDetails memberDetails = this.DeclaredMemberDetails[memberIndex];
            string name = this.StringStore[this.DeclaredMembers.GetNameIdentifier(memberIndex)].ToString();

            if (memberIndex == 0)
            {
                name = "[ROOT]";
            }
            else
            {
                name = ((char)memberDetails.Type).ToString() + " " + name;
            }

            sb.Append(new string('\t', indent));
            sb.AppendLine(name);

            indent++;
            int childIndex = this.DeclaredMembers.GetFirstChild(memberIndex);
            while (childIndex > 0)
            {
                WriteTree(sb, childIndex, indent);
                childIndex = this.DeclaredMembers.GetNextSibling(childIndex);
            }
        }
    }
}
