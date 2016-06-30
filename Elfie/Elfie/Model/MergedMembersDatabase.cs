// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    /// <summary>
    ///  MergedMembersDatabase is a single tree with public namespaces and types merged from multiple binaries. 
    ///  It could drive IntelliSense scenarios and is used to identify unique types when AddReferenceDatabase is built.
    /// </summary>
    public class MergedMembersDatabase
    {
        internal StringStore StringStore;
        internal ItemTree MergedMembers;
        internal PartialArray<int> MergedMemberSourcePackageIdentifier;
        internal PartialArray<int> MergedMemberDuplicateCount;

        public MergedMembersDatabase(StringStore sharedStore = null)
        {
            this.StringStore = sharedStore;
            if (this.StringStore == null) this.StringStore = new StringStore();

            this.MergedMembers = new ItemTree();
            this.MergedMemberSourcePackageIdentifier = new PartialArray<int>();
            this.MergedMemberSourcePackageIdentifier.Add(0);

            this.MergedMemberDuplicateCount = new PartialArray<int>();
            this.MergedMemberDuplicateCount.Add(0);
        }

        /// <summary>
        ///  Add public types from the given source PackageDatabase to this one.
        ///  Returns an array indicating which items were added. True means the
        ///  member or a descendant were unique. False means nothing was new.
        ///  Null means the type and descendants were excluded as non-public.
        /// </summary>
        /// <param name="source">PackageDatabase to add from</param>
        /// <returns>For each member, true if added, false if not unique, null if not a public type</returns>
        public DatabaseAddResult Add(PackageDatabase source, ArdbVersion ardbVersion)
        {
            // Merge namespaces into a common tree to find unique parts of this package
            DatabaseAddResult result = new DatabaseAddResult(source);
            AddUniqueMembers(source, 0, 0, result, ardbVersion);
            return result;
        }

        private bool AddUniqueMembers(
            PackageDatabase source,
            int sourceMemberIndex,
            int targetMemberIndex,
            DatabaseAddResult result,
            ArdbVersion version)
        {
            int pathLength = result.CurrentPath.Length;
            SymbolDetails memberDetails = source.DeclaredMemberDetails[sourceMemberIndex];
            bool hasUniqueDescendants = false;
            int indexToAddChildrenUnder = targetMemberIndex;

            // Add public types and namespaces which contain either
            bool addMember = false;
            if (memberDetails.Type == SymbolType.FrameworkTarget)
            {
                String8 encodedFrameworkNames = source.StringStore[source.DeclaredMembers.GetNameIdentifier(sourceMemberIndex)];
                result.CurrentFrameworkNames = new HashSet<string>(encodedFrameworkNames.ToString().ToFrameworkNames());
            }
            else if (memberDetails.Type == SymbolType.Namespace)
            {
                addMember = ContainsPublics(source, sourceMemberIndex);
            }
            else if (memberDetails.Type == SymbolType.Assembly)
            {
                if (result.CurrentFrameworkNames != null)
                {
                    result.CurrentFrameworkNames.Clear();
                }
            }
            else if (memberDetails.Type.IsType())
            {
                addMember = memberDetails.Modifiers.HasFlag(SymbolModifier.Public);
            }

            // Add the member if it is a public type or contains them [and set 'indexInTarget' to the added member]
            if (addMember)
            {
                result.PublicTypeCount++;

                // Remap name for target StringStore [all unique names will be added anyway]
                int memberName = source.DeclaredMembers.GetNameIdentifier(sourceMemberIndex);
                String8 memberNameText = source.StringStore[memberName];
                result.CurrentPath.Append((result.CurrentPath.Length > 0 ? "." : "") + memberNameText.ToString());
                memberName = this.StringStore.FindOrAddString(memberNameText);

                string fullTypeName;
                HashSet<string> frameworkTargets;

                // See if this name is already found in the merged namespace tree
                if (!this.MergedMembers.TryFindChildByName(targetMemberIndex, memberName, out indexToAddChildrenUnder))
                {
                    // If not, add it, and the tree is unique because this name is
                    indexToAddChildrenUnder = this.MergedMembers.Add(targetMemberIndex, memberName);

                    // Identify the package which added it
                    int packageNameIdentifier = this.StringStore.FindOrAddString(source.Identity.PackageName);
                    this.MergedMemberSourcePackageIdentifier.Add(packageNameIdentifier);
                    this.MergedMemberDuplicateCount.Add(1);

                    hasUniqueDescendants = true;
                    result.MergedTypeCount++;

                    if (version != ArdbVersion.V1 && memberDetails.Type.IsType())
                    {
                        // We have encountered a fully-qualified type name for the 
                        // very first time. We will associate this name with the
                        // current framework target. We will also remember this
                        // sourceMemberIndex, in order to use it as the item
                        // to add when populating the ARDB.
                        fullTypeName = result.CurrentPath.ToString();
                        result.TypeToFrameworkTargetsMap.Add(fullTypeName, new HashSet<string>());

                        if (result.CurrentFrameworkNames != null)
                        {
                            result.TypeToFrameworkTargetsMap[fullTypeName].UnionWith(result.CurrentFrameworkNames);
                        }

                        result.TypeNameToTypeIndexMap.Add(fullTypeName, sourceMemberIndex);
                    }
                }
                else
                {
                    // Otherwise, if this is a duplicate with another entry in the same package, still include it (different framework targets)
                    String8 sourcePackage = this.StringStore[this.MergedMemberSourcePackageIdentifier[indexToAddChildrenUnder]];
                    if (version != ArdbVersion.V1 &&
                        result.CurrentFrameworkNames != null &&
                        sourcePackage.Equals(source.Identity.PackageName))
                    {
                        hasUniqueDescendants = true;
                        result.MergedTypeCount++;

                        if (version != ArdbVersion.V1 && memberDetails.Type.IsType())
                        {
                            // We have encountered a fully-qualified type name that we've seen
                            // before. We will record the current framework target but 
                            // will already have a source member index to use to add
                            // to the ARDB later.
                            fullTypeName = result.CurrentPath.ToString();
                            frameworkTargets = result.TypeToFrameworkTargetsMap[fullTypeName];
                            frameworkTargets.UnionWith(result.CurrentFrameworkNames);
                            Debug.Assert(result.TypeNameToTypeIndexMap.ContainsKey(fullTypeName));
                        }
                    }
                    else
                    {
                        // If this isn't unique, increment the count of copies
                        this.MergedMemberDuplicateCount[indexToAddChildrenUnder] += 1;
                    }
                }
            }

            if (!memberDetails.Type.IsType())
            {
                int childIndex = source.DeclaredMembers.GetFirstChild(sourceMemberIndex);
                while (childIndex > 0)
                {
                    hasUniqueDescendants |= AddUniqueMembers(source, childIndex, indexToAddChildrenUnder, result, version);
                    childIndex = source.DeclaredMembers.GetNextSibling(childIndex);
                }
            }

            // Add the package which had this member to results if it wasn't unique
            if (hasUniqueDescendants == false)
            {
                result.SourcePackageNames[sourceMemberIndex] = this.StringStore[this.MergedMemberSourcePackageIdentifier[indexToAddChildrenUnder]];
            }

            // Record whether this member was unique (and added)
            result.WasMemberAdded[sourceMemberIndex] = hasUniqueDescendants;
            result.CurrentPath.Length = pathLength;
            return hasUniqueDescendants;
        }

        private bool ContainsPublics(IMemberDatabase source, int index)
        {
            SymbolType type = source.GetMemberType(index);
            SymbolModifier modifier = source.GetMemberModifiers(index);

            // If this is a public type or extension method, we contain them
            if ((type.IsType() || type.IsExtensionMethod()) && modifier.HasFlag(SymbolModifier.Public)) return true;

            // If any descendants contain public types, we contain them
            int childIndex = source.DeclaredMembers.GetFirstChild(index);
            while (childIndex > 0)
            {
                if (ContainsPublics(source, childIndex)) return true;
                childIndex = source.DeclaredMembers.GetNextSibling(childIndex);
            }

            // Otherwise, we don't
            return false;
        }

        public void ConvertToImmutable()
        {
            if (this.StringStore.ConvertToImmutable())
            {
                this.MergedMembers.UpdateIdentifiers(this.StringStore);
                this.MergedMembers.Sort((left, right) => this.MergedMembers.GetNameIdentifier(left).CompareTo(this.MergedMembers.GetNameIdentifier(right)));
            }
        }

        /// <summary>
        ///  Debugging aid which writes each member of the merged tree and the package which
        ///  added it.
        /// </summary>
        /// <param name="writer">TextWriter to write the tree to</param>
        public void WriteMergedTree(TextWriter writer)
        {
            ConvertToImmutable();

            this.MergedMembers.WriteTree(writer, this.StringStore, 0, -1, (index, w) =>
            {
                w.Write('\t');
                this.StringStore[this.MergedMemberSourcePackageIdentifier[index]].WriteTo(w);
            });
        }

        /// <summary>
        ///  Debugging aid which writes each full name and the number of occurrences of it
        /// </summary>
        /// <param name="writer">TextWriter to write the tree to</param>
        public void WriteDuplicateCounts(TextWriter writer)
        {
            ConvertToImmutable();

            writer.WriteLine("FullName,OccurrenceCount");

            for (int i = 0; i < this.MergedMembers.Count; ++i)
            {
                int duplicateCount = this.MergedMemberDuplicateCount[i];
                if (duplicateCount > 1)
                {
                    Path8 fullName = new Path8(this.StringStore, this.MergedMembers, i, '.', this.MergedMembers.GetDepth(i) - 1);
                    fullName.WriteTo(writer);
                    writer.Write(",");
                    writer.Write(duplicateCount);
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        ///  Debugging aid which writes each full name and the number of occurrences of it
        /// </summary>
        /// <param name="writer">TextWriter to write the tree to</param>
        public void WriteDuplicateHistogram(TextWriter writer)
        {
            ConvertToImmutable();

            Dictionary<int, int> memberCountByOccurrenceCount = new Dictionary<int, int>();
            for (int i = 0; i < this.MergedMemberDuplicateCount.Count; ++i)
            {
                int duplicateCount = this.MergedMemberDuplicateCount[i];

                int countForThisDuplicateCount;
                if (!memberCountByOccurrenceCount.TryGetValue(duplicateCount, out countForThisDuplicateCount))
                {
                    countForThisDuplicateCount = 0;
                }

                memberCountByOccurrenceCount[duplicateCount] = countForThisDuplicateCount + 1;
            }

            writer.WriteLine("OccurrenceCount,MemberCount");
            foreach (int duplicateCount in memberCountByOccurrenceCount.Keys)
            {
                writer.Write(duplicateCount);
                writer.Write(",");
                writer.Write(memberCountByOccurrenceCount[duplicateCount]);
                writer.WriteLine();
            }
        }

        /// <summary>
        ///  Debugging aid which writes each full name and the number of occurrences of it
        /// </summary>
        /// <param name="writer">TextWriter to write the tree to</param>
        public void WriteDuplicateWeight(TextWriter writer)
        {
            ConvertToImmutable();

            // Walk and compute total weight (duplicates x members) for the tree
            int[] duplicateWeight = new int[this.MergedMembers.Count];
            DuplicateWeight(0, duplicateWeight);

            // Write types/namespaces with over 1,000 weight
            writer.WriteLine("FullName,Count,Weight");

            for (int i = 0; i < this.MergedMembers.Count; ++i)
            {
                if (duplicateWeight[i] > 1000)
                {
                    Path8 fullName = new Path8(this.StringStore, this.MergedMembers, i, '.', this.MergedMembers.GetDepth(i));
                    fullName.WriteTo(writer);
                    writer.Write(",");
                    writer.Write(this.MergedMemberDuplicateCount[i]);
                    writer.Write(",");
                    writer.Write(duplicateWeight[i]);
                    writer.WriteLine();
                }
            }
        }

        public int DuplicateWeight(int index, int[] duplicateWeight)
        {
            int totalFromDescendants = 0;

            int child = this.MergedMembers.GetFirstChild(index);
            while (child > 0)
            {
                totalFromDescendants += DuplicateWeight(child, duplicateWeight);
                child = this.MergedMembers.GetNextSibling(child);
            }

            totalFromDescendants += this.MergedMemberDuplicateCount[index];
            duplicateWeight[index] = totalFromDescendants;

            return totalFromDescendants;
        }
    }
}
