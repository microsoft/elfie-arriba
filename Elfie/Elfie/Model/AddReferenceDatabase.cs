// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    ///  AddReferenceDatabase is a scaled down member database designed for the
    ///  Add Reference scenario. It contains public namespaces and classes from
    ///  many source packages and is used to identify the package containing a
    ///  given type as efficiently as possible.
    /// 
    ///  The DeclaredMembers tree contains package details so that they don't 
    ///  add to each node size.
    /// 
    ///  The Tree is Package -> Binary -> Namespace* -> Type*
    /// </summary>
    public class AddReferenceDatabase : IStatistics, IBinarySerializable, IMemberDatabase
    {
        public static readonly string TextFilePrefix = "Elfie V";

        // NOTE: Update these when the binary or text formats change so old code doesn't try to load newer content and vice versa.
        public int GetFileFormatVersion()
        {
            switch (this.DatabaseFormatVersion)
            {
                case ArdbVersion.V1:
                    {
                        return 1;
                    }

                case ArdbVersion.Current:
                    {
                        return 2;
                    }
            }
            // Unrecognized database version '{0}'.
            throw new InvalidOperationException(string.Format(Resources.UnrecognizedDatabaseFormatVersion, this.DatabaseFormatVersion));
        }

        public ArdbVersion DatabaseFormatVersion { get; set; }
        public int DatabaseVersion { get; set; }
        public StringStore StringStore { get; private set; }
        public ItemTree DeclaredMembers { get; private set; }
        public IList<SymbolType> DeclaredMemberTypes { get; private set; }
        public MemberIndex Index { get; private set; }

        private MergedMembersDatabase _mergedMembers;

        public AddReferenceDatabase() : this(ArdbVersion.Current)
        {
        }

        public AddReferenceDatabase(ArdbVersion ardbVersion)
        {
            this.DatabaseFormatVersion = ardbVersion;
            this.StringStore = new StringStore();
            this.DeclaredMembers = new ItemTree();

            this.DeclaredMemberTypes = new List<SymbolType>();
            this.DeclaredMemberTypes.Add(SymbolType.Package);

            this.Index = new MemberIndex();

            _mergedMembers = new MergedMembersDatabase(this.StringStore);
        }

        #region Conversion from PackageDatabase
        /// <summary>
        ///  Add all members from the PackageDatabase to this AddReferenceDatabase.
        ///  PackageDatabases should be added in ranked order (ex: descending download count)
        ///  so that the highest ranked source will be shown first.
        /// </summary>
        /// <param name="source">PackageDatabase to include unique public types from</param>
        public void AddReferenceAssemblyTypes(PackageDatabase source)
        {
            // This helper imports all types in the source, included duplicate items.
            // Framework targets are ignored.
            // Assemblies are included.
            ImportPublics(
                source,
                sourceMemberIndex: 0,
                targetMemberIndex: 0,
                uniqueMembers: null,
                ardbVersion: this.DatabaseFormatVersion);
        }

        /// <summary>
        ///  Add unique members from the PackageDatabase to this v1 AddReferenceDatabase.
        ///  PackageDatabases should be added in ranked order (ex: descending download count)
        ///  so that the highest ranked source will be shown for a given unique
        ///  fully qualified type name.
        /// </summary>
        /// <param name="source">PackageDatabase to include unique public types from</param>
        /// <returns>DatabaseAddResult describing what was added</returns>
        /// <summary>
        public DatabaseAddResult AddUniqueMembers(PackageDatabase source)
        {
            // Merge namespaces into a common tree to find unique parts of this package
            DatabaseAddResult result = _mergedMembers.Add(source, this.DatabaseFormatVersion);

            if (this.DatabaseFormatVersion == ArdbVersion.V1)
            {
                // Import everything which was unique
                ImportPublics(
                    source,
                    sourceMemberIndex: 0,
                    targetMemberIndex: 0,
                    uniqueMembers: result.WasMemberAdded,
                    ardbVersion: ArdbVersion.V1);
            }
            else
            {
                // Import everything which was unique. If we've got no
                // type information, nothing new was observed on merge
                if (result.TypeNameToTypeIndexMap.Count > 0)
                {
                    int packageNodeIndex = ImportPackageNodes(source);
                    ImportFrameworkTargets(source, packageNodeIndex, result);
                }
            }

            return result;
        }

        private int ImportPackageNodes(PackageDatabase source)
        {
            int packageNodeIndex, sourceMemberIndex, targetMemberIndex;

            sourceMemberIndex = targetMemberIndex = 0; // 0 == sentinel root
            sourceMemberIndex = source.DeclaredMembers.GetFirstChild(sourceMemberIndex);
            SymbolDetails memberDetails = source.DeclaredMemberDetails[sourceMemberIndex];

            int memberName = source.DeclaredMembers.GetNameIdentifier(sourceMemberIndex);
            memberName = this.StringStore.FindOrAddString(source.StringStore[memberName]);
            targetMemberIndex = this.DeclaredMembers.Add(targetMemberIndex, memberName);
            this.DeclaredMemberTypes.Add(memberDetails.Type);

            packageNodeIndex = targetMemberIndex;

            Debug.Assert(memberDetails.Type == SymbolType.Package);
            Debug.Assert(source.DeclaredMembers.GetNextSibling(sourceMemberIndex) < 0);

            // Add optional pre-release node
            if (source.Identity.ReleaseName.IsPrereleaseVersion())
            {
                int packageReleaseId = this.StringStore.FindOrAddString(source.Identity.ReleaseName);
                this.DeclaredMembers.Add(targetMemberIndex, packageReleaseId);
                this.DeclaredMemberTypes.Add(SymbolType.Version);
            }

            // Inject popularity rank as an sibling node of package contents
            int popularityRank = source.GetPopularityRank(sourceMemberIndex);
            int popularityRankId = this.StringStore.FindOrAddString(popularityRank.ToString());
            this.DeclaredMembers.Add(targetMemberIndex, popularityRankId);
            this.DeclaredMemberTypes.Add(SymbolType.PopularityRank);

            return packageNodeIndex;
        }

        private void ImportFrameworkTargets(PackageDatabase source, int packageMemberIndex, DatabaseAddResult databaseAddResult)
        {
            Dictionary<string, HashSet<int>> frameworkTargetsToIncludedTypes = new Dictionary<string, HashSet<int>>();
            HashSet<int> frameworkTargetTypes;

            foreach (string type in Sorted(databaseAddResult.TypeToFrameworkTargetsMap.Keys))
            {
                HashSet<string> frameworkTargetsSet = databaseAddResult.TypeToFrameworkTargetsMap[type];
                string[] frameworkTargets = new string[frameworkTargetsSet.Count];
                frameworkTargetsSet.CopyTo(frameworkTargets, 0);
                Array.Sort(frameworkTargets);

                string sortedFrameworkTargets = StringExtensions.EncodeFrameworkNamesToXml(frameworkTargets);

                if (!frameworkTargetsToIncludedTypes.TryGetValue(sortedFrameworkTargets, out frameworkTargetTypes))
                {
                    frameworkTargetTypes = frameworkTargetsToIncludedTypes[sortedFrameworkTargets] = new HashSet<int>();
                }
                frameworkTargetTypes.Add(databaseAddResult.TypeNameToTypeIndexMap[type]);
            }

            foreach (string sortedFrameworkTargets in frameworkTargetsToIncludedTypes.Keys)
            {
                int frameworkTargetsId = this.StringStore.FindOrAddString(sortedFrameworkTargets);
                int frameworkTargetIndex = this.DeclaredMembers.Add(packageMemberIndex, frameworkTargetsId);
                this.DeclaredMemberTypes.Add(SymbolType.FrameworkTarget);

                // Now we'll add all types associated with this framework target set
                // To do so, we grab a single example from the source that expresses
                // the fully-qualified name, walk its tree, and add it to the target
                frameworkTargetTypes = frameworkTargetsToIncludedTypes[sortedFrameworkTargets];

                Stack<int> nodes = new Stack<int>();
                foreach (int typeIndex in frameworkTargetTypes)
                {
                    int sourceMemberIndex = typeIndex;
                    SymbolDetails details = source.DeclaredMemberDetails[sourceMemberIndex];

                    while (details.Type == SymbolType.Namespace || details.Type.IsType())
                    {
                        nodes.Push(sourceMemberIndex);
                        sourceMemberIndex = source.DeclaredMembers.GetParent(sourceMemberIndex);
                        details = source.DeclaredMemberDetails[sourceMemberIndex];
                    }

                    int targetMemberIndex = frameworkTargetIndex;
                    while (nodes.Count > 0)
                    {
                        sourceMemberIndex = nodes.Pop();
                        details = source.DeclaredMemberDetails[sourceMemberIndex];
                        int memberName = source.DeclaredMembers.GetNameIdentifier(sourceMemberIndex);
                        memberName = this.StringStore.FindOrAddString(source.StringStore[memberName]);

                        int indexInTarget;
                        if (!this.DeclaredMembers.TryFindChildByName(targetMemberIndex, memberName, out indexInTarget))
                        {
                            targetMemberIndex = this.DeclaredMembers.Add(targetMemberIndex, memberName);
                            this.Index.AddItem(memberName, targetMemberIndex);
                            this.DeclaredMemberTypes.Add(details.Type);
                        }
                        else
                        {
                            targetMemberIndex = indexInTarget;
                        }
                    }
                }
            }
        }

        private IEnumerable<string> Sorted(IEnumerable<string> strings)
        {
            var result = new List<string>();
            foreach (string item in strings) { result.Add(item); }
            result.Sort();
            return result;
        }

        private void ImportPublics(
            PackageDatabase source,
            int sourceMemberIndex,
            int targetMemberIndex,
            bool?[] uniqueMembers,
            ArdbVersion ardbVersion)
        {
            SymbolDetails memberDetails = source.DeclaredMemberDetails[sourceMemberIndex];

            // Skip adding any subtrees which are not unique
            if (uniqueMembers != null)
            {
                bool? wasUnique = uniqueMembers[sourceMemberIndex];
                if (wasUnique == null)
                {
                    // Not added because not a public type (or ancestor wasn't)
                    return;
                }
                else if (wasUnique == false)
                {
                    // Not added because a non-unique item was detected for a package db
                    // that is not permitted to add duplicates (only MS frameworks pkgs should).
                    return;
                }
            }

            int indexInTarget = targetMemberIndex;

            // Index everything except the sentinel root. Also skip assembly nodes,
            // as matches will occur at the package node (which will actually 
            // represent either a package or a reference assembly).
            if (sourceMemberIndex > 0 && (ardbVersion != ArdbVersion.V1 || memberDetails.Type != SymbolType.FrameworkTarget))
            {
                // Remap name for target StringStore
                int memberName = source.DeclaredMembers.GetNameIdentifier(sourceMemberIndex);
                memberName = this.StringStore.FindOrAddString(source.StringStore[memberName]);

                // Add this member if it doesn't already exist [ex: same namespaces in multiple binaries in Package]
                if (!this.DeclaredMembers.TryFindChildByName(targetMemberIndex, memberName, out indexInTarget))
                {
                    // Add member to target tree
                    indexInTarget = this.DeclaredMembers.Add(targetMemberIndex, memberName);
                    this.DeclaredMemberTypes.Add(memberDetails.Type);

                    // Validate DeclaredMembers and DeclaredMemberTypes are in sync
                    if (this.DeclaredMemberTypes.Count != this.DeclaredMembers.Count) throw new InvalidOperationException(String.Format(Resources.DatabaseArraysOutOfSync, "DeclaredMemberTypes"));

                    // Add member to index [types and namespaces so 'find second-to-last value' search works]
                    this.Index.AddItem(memberName, indexInTarget);

                    if (memberDetails.Type == SymbolType.Package)
                    {
                        int addedNode;

                        // If this package is a pre-release version, we will introduce
                        // a new node that holds the specific release details. This is
                        // a short-term fix required to allow a fixer to request installation
                        // of a pre-release only package. Nuget will add a new API in the future
                        // that prevents the need for this node (which we can then remove).
                        if (source.Identity.ReleaseName.IsPrereleaseVersion())
                        {
                            int packageReleaseId = this.StringStore.FindOrAddString(source.Identity.ReleaseName);
                            addedNode = this.DeclaredMembers.Add(indexInTarget, packageReleaseId);

                            if (ardbVersion == ArdbVersion.V1)
                            {
                                // For V1, we injected the prerelease node as a parent to all 
                                // following nodes. For post-V1, this node is a sibling with
                                // popularity rank.
                                indexInTarget = addedNode;
                            }

                            this.DeclaredMemberTypes.Add(SymbolType.Version);
                        }

                        // Inject popularity rank as an sibling node of package contents
                        int popularityRank = source.GetPopularityRank(sourceMemberIndex);
                        int popularityRankId = this.StringStore.FindOrAddString(popularityRank.ToString());
                        addedNode = this.DeclaredMembers.Add(indexInTarget, popularityRankId);

                        if (ardbVersion == ArdbVersion.V1)
                        {
                            // For V1, we injected the popularity rank directly underneath the
                            // package or pre-release node. Post-V1, this node is a sibling of
                            // the target frameworks node.
                            // popularity rank.
                            indexInTarget = addedNode;
                        }

                        this.DeclaredMemberTypes.Add(SymbolType.PopularityRank);
                    }
                }
                else if (ardbVersion == ArdbVersion.V1)
                {
                    if (memberDetails.Type == SymbolType.Package)
                    {
                        // For V1, we need to skip injected popularity node and prerelease nodes
                        // when looking for duplicate children. This isn't required for post-V1, as
                        // these nodes are introduced as siblings of the TFM and/or assembly-level data. 
                        indexInTarget = this.DeclaredMembers.GetFirstChild(indexInTarget);

                        SymbolType symbolType = this.GetMemberType(indexInTarget);
                        if (symbolType == SymbolType.Version)
                        {
                            indexInTarget = this.DeclaredMembers.GetFirstChild(indexInTarget);
                        }

                        // At this point, we should always have the popularity rank in hand
                        Debug.Assert(this.GetMemberType(indexInTarget) == SymbolType.PopularityRank);
                    }
                    else if (memberDetails.Type == SymbolType.FrameworkTarget)
                    {
                        // If we find a TFM node, we will skip past it;
                        indexInTarget = this.DeclaredMembers.GetFirstChild(indexInTarget);
                    }
                }
            }

            // Recurse on children down to type level. We do not walk type members.
            // This means that we will exclude public nested types from the database.
            if (!memberDetails.Type.IsType())
            {
                int childIndex = source.DeclaredMembers.GetFirstChild(sourceMemberIndex);
                while (childIndex > 0)
                {
                    ImportPublics(source, childIndex, indexInTarget, uniqueMembers, ardbVersion);
                    childIndex = source.DeclaredMembers.GetNextSibling(childIndex);
                }
            }
        }
        #endregion

        public Symbol QueryRoot
        {
            get { return new Symbol(this, 0); }
        }

        #region IMemberDatabase

        public String8 GetPackageName(int index)
        {
            int ancestor;

            if (this.TryGetAncestorOfType(index, SymbolType.Package, out ancestor))
            {
                return this.StringStore[this.DeclaredMembers.GetNameIdentifier(ancestor)];
            }

            return String8.Empty;
        }

        public String8 GetPackageVersion(int index)
        {
            int ancestor;

            if (this.TryGetAncestorOfType(index, SymbolType.Package, out ancestor))
            {
                int versionId;

                if (this.TryGetFirstChildOfType(ancestor, SymbolType.Version, out versionId))
                {
                    return this.StringStore[this.DeclaredMembers.GetNameIdentifier(versionId)];
                }
            }
            return String8.Empty;
        }

        public int GetPopularityRank(int index)
        {
            int ancestor, popularityRank;

            if (this.TryGetAncestorOfType(index, SymbolType.Package, out ancestor))
            {
                if (this.TryGetFirstChildOfType(ancestor, SymbolType.PopularityRank, out popularityRank))
                {
                    return int.Parse(this.StringStore[this.DeclaredMembers.GetNameIdentifier(popularityRank)].ToString());
                }
            }
            return 0;
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
            return this.DeclaredMemberTypes[memberIndex];
        }

        public SymbolModifier GetMemberModifiers(int memberIndex)
        {
            // All items are public
            return SymbolModifier.Public;
        }

        public String8 GetMemberParameters(int memberIndex)
        {
            // No signature information available
            return String8.Empty;
        }

        public Path8 GetMemberFilePath(int memberIndex)
        {
            // No location information available
            return Path8.Empty;
        }

        public ushort GetMemberFileLine(int memberIndex)
        {
            // No location information available
            return 0;
        }

        public ushort GetMemberCharInLine(int memberIndex)
        {
            // No location information available
            return 0;
        }

        public void SetLocation(int memberIndex, string filePath, ushort line, ushort charInLine)
        {
            // Locations not available; can't update them
            throw new NotImplementedException();
        }
        #endregion

        #region IStatistics
        public int Count
        {
            get { return DeclaredMembers.Count; }
        }

        public long Bytes
        {
            get { return this.StringStore.Bytes + this.DeclaredMembers.Bytes + this.Index.Bytes + this.DeclaredMemberTypes.Count; }
        }
        #endregion

        #region IBinarySerializable
        /// <summary>
        ///  Convert this AddReferenceDatabase from the mutable (buildable) form into the immutable (queryable)
        ///  form. This must be done after all PackageDatabases are added and before any queriesa are run.
        ///  When deserialized, ARDB is always loaded in queryable form.
        /// </summary>
        public void ConvertToImmutable()
        {
            this.StringStore.ConvertToImmutable();

            if (this.DeclaredMembers.RequiresRemap())
            {
                this.DeclaredMembers.UpdateIdentifiers(this.StringStore);

                this.DeclaredMembers.Sort((left, right) =>
                {
                    if (DeclaredMembers.GetParent(left) == 0 && DeclaredMembers.GetParent(right) == 0)
                    {
                        // Keep Packages sorted by insertion order (popularity)
                        return left.CompareTo(right);
                    }
                    else
                    {
                        // Sort namespaces and types by name [same sort as StringStore]
                        return DeclaredMembers.GetNameIdentifier(left).CompareTo(DeclaredMembers.GetNameIdentifier(right));
                    }
                });
            }

            this.Index.ConvertToImmutable(this.StringStore);
        }

        /// <summary>
        ///  Write this database in binary form to the given BinaryWriter.
        /// </summary>
        /// <param name="w">BinaryWriter to write to</param>
        public void WriteBinary(BinaryWriter w)
        {
            ConvertToImmutable();

            w.Write(GetFileFormatVersion());
            w.Write(this.DatabaseVersion);

            this.StringStore.WriteBinary(w);
            this.DeclaredMembers.WriteBinary(w);
            this.Index.WriteBinary(w);

            w.Write(this.DeclaredMemberTypes.Count);
            for (int i = 0; i < this.DeclaredMemberTypes.Count; ++i)
            {
                w.Write((byte)this.DeclaredMemberTypes[i]);
            }
        }

        /// <summary>
        ///  Load this database from the given BinaryReader, replacing existing state.
        /// </summary>
        /// <param name="r">BinaryReader to read from</param>
        public void ReadBinary(BinaryReader r)
        {
            int formatVersion = r.ReadInt32();

            int expectedArdbVersion = GetFileFormatVersion();

            if (formatVersion != expectedArdbVersion)
            {
                throw new IOException(String.Format(Resources.DatabaseFormatVersionWrong, expectedArdbVersion, formatVersion));
            }

            this.DatabaseVersion = r.ReadInt32();

            this.StringStore = new StringStore();
            this.StringStore.ReadBinary(r);

            this.DeclaredMembers = new ItemTree();
            this.DeclaredMembers.ReadBinary(r);

            this.Index = new MemberIndex();
            this.Index.ReadBinary(r);

            int declaredMemberTypeCount = r.ReadArrayLength(1);
            this.DeclaredMemberTypes = new SymbolType[declaredMemberTypeCount];
            for (int i = 0; i < declaredMemberTypeCount; ++i)
            {
                this.DeclaredMemberTypes[i] = (SymbolType)r.ReadByte();
            }
        }
        #endregion

        #region Text Serialization
        /// <summary>
        ///  Write this database in text form. The text form is recommended for sending the data to another machine.
        ///  The text format is smaller (no index data) and more change tolerant (fewer data structure details in the output).
        ///  
        ///  NOTE: Increment TextFileFormatVersion when breaking changes are made.
        /// </summary>
        /// <param name="w">TextWriter to write to</param>
        public void WriteText(TextWriter w)
        {
            WriteText(w, null);
        }

        /// <summary>
        ///  Write this database in text form. The text form is recommended for sending the data to another machine.
        ///  The text format is smaller (no index data) and more change tolerant (fewer data structure details in the output).
        /// </summary>
        /// <param name="w">TextWriter to write to</param>
        /// <param name="includeSymbol">Method returning whether to include the Symbol and descendants in output</param>
        public void WriteText(TextWriter w, Func<Symbol, bool> includeSymbol)
        {
            ConvertToImmutable();

            w.Write(TextFilePrefix);
            w.WriteLine(GetFileFormatVersion());
            w.WriteLine(this.DatabaseVersion);
            WriteText(w, 0, -1, includeSymbol);
        }

        private void WriteText(TextWriter w, int index, int indent, Func<Symbol, bool> includeSymbol = null)
        {
            if (includeSymbol != null)
            {
                if (!includeSymbol(new Symbol(this, index))) return;
            }

            if (indent >= 0)
            {
                for (int i = 0; i < indent; ++i)
                {
                    w.Write("\t");
                }

                w.Write((char)this.DeclaredMemberTypes[index]);
                w.Write(" ");
                this.StringStore[this.DeclaredMembers.GetNameIdentifier(index)].WriteTo(w);
                w.WriteLine();
            }

            int child = this.DeclaredMembers.GetFirstChild(index);
            while (child > 0)
            {
                WriteText(w, child, indent + 1, includeSymbol);
                child = this.DeclaredMembers.GetNextSibling(child);
            }
        }

        /// <summary>
        ///  Load this database from the text format. Text format loading is slower, so the database should be
        ///  cached locally in the Binary Format for faster loading.
        ///  
        ///  NOTE: Increment TextFileFormatVersion when breaking changes are made.
        /// </summary>
        /// <param name="r">TextReader to read from</param>
        public void ReadText(TextReader r)
        {
            // Reset internal data structures
            this.StringStore = new StringStore();
            this.DeclaredMembers = new ItemTree();
            this.Index = new MemberIndex();

            // Read and verify the file format and database versions
            Tuple<int, int> versions = ReadVersionsOnly(r);
            int expectedVersion = GetFileFormatVersion();
            if (!expectedVersion.Equals(versions.Item1))
            {
                throw new IOException(String.Format(Resources.DatabaseFormatVersionWrong, expectedVersion, versions.Item1));
            }

            this.DatabaseVersion = versions.Item2;

            // Read the member tree
            Stack<int> currentPath = new Stack<int>();
            currentPath.Push(0);

            int lineNumber = 1;

            string lineString;
            while (true)
            {
                lineString = r.ReadLine();
                if (lineString == null) break;
                lineNumber++;

                // Read depth, type, and name of member
                int charIndex = 0;
                for (; charIndex < lineString.Length && lineString[charIndex] == '\t'; ++charIndex) ;
                int depth = charIndex + 1;

                SymbolType type = (SymbolType)(byte)lineString[charIndex];
                AssertCharacter(lineString, lineNumber, charIndex + 1, ' ');
                charIndex += 2;

                string name = lineString.Substring(charIndex, lineString.Length - charIndex);

                // If the SymbolType isn't one we use in ARDB (or is unknown), skip loading this member [forward compat]
                if (!IsArdbSymbolType(type))
                {
                    continue;
                }

                // Pop the path stack to the parent of the member
                while (currentPath.Count > depth)
                {
                    currentPath.Pop();
                }

                // If ancestors are missing (excluded lines), put included elements under the last valid ancestor
                while (currentPath.Count < depth)
                {
                    currentPath.Push(currentPath.Peek());
                }

                // Add this member to the tree, add the type, and add the string
                int nameIdentifier = this.StringStore.FindOrAddString(name);
                int newMemberIdentifier = this.DeclaredMembers.Add(currentPath.Peek(), nameIdentifier);
                this.DeclaredMemberTypes.Add(type);

                // Add this member name to the index
                this.Index.AddItem(nameIdentifier, newMemberIdentifier);

                // Push this item as a potential parent
                currentPath.Push(newMemberIdentifier);
            }

            // Convert to Queryable form
            this.ConvertToImmutable();
        }

        public static Tuple<int, int> ReadVersionsOnly(TextReader r)
        {
            int formatVersion;
            int databaseVersion;

            // Read the first line [Format Version] and extract format version integer
            string formatVersionLine = r.ReadLine();
            if (!formatVersionLine.StartsWith(TextFilePrefix)) throw new IOException(String.Format(Resources.DatabaseFormatVersionWrong, TextFilePrefix, formatVersionLine));

            string formatVersionString = formatVersionLine.Substring(TextFilePrefix.Length);
            if (!int.TryParse(formatVersionString, out formatVersion)) throw new IOException(String.Format(Resources.DatabaseVersionNotInteger, formatVersionString));

            // Read the second line [Database Version] and extract database version integer
            string versionLine = r.ReadLine();
            if (!int.TryParse(versionLine, out databaseVersion))
            {
                throw new IOException(String.Format(Resources.DatabaseVersionNotInteger, versionLine));
            }

            return new Tuple<int, int>(formatVersion, databaseVersion);
        }

        private bool IsArdbSymbolType(SymbolType type)
        {
            return type.IsAboveType() || type.IsType() || type.IsExtensionMethod();
        }

        private void AssertCharacter(string lineString, int lineNumber, int charIndex, char expected)
        {
            if (lineString.Length <= charIndex) throw new IOException(String.Format(Resources.DatabaseLineTooShort, expected, lineNumber, charIndex + 1, lineString.Length));

            char actual = lineString[charIndex];
            if (actual != expected) throw new IOException(String.Format(Resources.DatabaseCharacterUnexpected, expected, lineNumber, charIndex + 1, actual));
        }
        #endregion

        #region Debuggability
        public MergedMembersDatabase GetMergedMembers()
        {
            // Throw if unable to return merged tree [on reload]. Merged tree is only available while adding databases during crawl.
            if (_mergedMembers == null) throw new InvalidOperationException();
            return _mergedMembers;
        }

        public override string ToString()
        {
            return Write.ToString(WriteText);
        }
        #endregion
    }
}
