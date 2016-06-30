// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public class DatabaseAddResult
    {
        public PackageDatabase Source;
        public int PublicTypeCount;
        public int MergedTypeCount;

        public bool?[] WasMemberAdded;
        public String8[] SourcePackageNames;

        public StringBuilder CurrentPath;
        public HashSet<string> CurrentFrameworkNames;
        public Dictionary<string, int> TypeNameToTypeIndexMap;
        public Dictionary<string, HashSet<string>> TypeToFrameworkTargetsMap;

        public DatabaseAddResult(PackageDatabase source)
        {
            this.Source = source;
            this.CurrentPath = new StringBuilder();
            this.WasMemberAdded = new bool?[source.DeclaredMembers.Count];
            this.SourcePackageNames = new String8[source.DeclaredMembers.Count];
            this.TypeNameToTypeIndexMap = new Dictionary<string, int>();
            this.TypeToFrameworkTargetsMap = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        ///  Debugging aid which writes each member in the source tree (indented) along with whether
        ///  it was added (+), non-unique (-), or skipped (0). Used to see visually what was included 
        ///  and excluded from a given source.
        /// </summary>
        /// <param name="writer">TextWriter to log to</param>
        public void WriteMemberResults(TextWriter writer)
        {
            this.Source.DeclaredMembers.WriteTree(writer, this.Source.StringStore, 0, -1, (index, w) =>
            {
                bool? wasAdded = this.WasMemberAdded[index];
                if (wasAdded == null) w.Write(" 0");
                if (wasAdded == false) w.Write(" -");
                if (wasAdded == true) w.Write(" +");

                if (wasAdded == false)
                {
                    w.Write(" [");
                    this.SourcePackageNames[index].WriteTo(w);
                    w.Write("]");
                }
            });
        }

        /// <summary>
        ///  Debugging aid which writes each root which was not included from a given source because it was
        ///  a duplicate.
        /// </summary>
        /// <param name="writer">TextWriter to log to</param>
        public void WriteDuplicateComponents(TextWriter writer)
        {
            if (this.WasMemberAdded[0] == false)
            {
                writer.Write("Everything was excluded.");
            }
            else
            {
                WriteDuplicateTree(writer, 0);
            }
        }

        private void WriteDuplicateTree(TextWriter writer, int index = 0)
        {
            bool wasExcluded = this.WasMemberAdded[index].HasValue && this.WasMemberAdded[index].Value == false;

            if (wasExcluded)
            {
                // If this member was excluded, write the path of it only (and not the subtree)
                this.Source.DeclaredMembers.GetPath(index, this.Source.StringStore, '.').WriteTo(writer);
                writer.Write("\t[");
                this.SourcePackageNames[index].WriteTo(writer);
                writer.WriteLine("]");
            }
            else
            {
                // If it was not excluded, recurse looking for excluded parts
                int childIndex = this.Source.DeclaredMembers.GetFirstChild(index);
                while (childIndex > 0)
                {
                    WriteDuplicateTree(writer, childIndex);
                    childIndex = this.Source.DeclaredMembers.GetNextSibling(childIndex);
                }
            }
        }
    }
}
