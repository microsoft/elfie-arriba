// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Index;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public interface IMemberDatabase : IStatistics
    {
        /// <summary>
        ///  StringStore contains all unique strings in the database and provides
        ///  the ability to find them. [Required]
        /// </summary>
        StringStore StringStore { get; }

        /// <summary>
        ///  DeclaredMembers contains the tree of members declared in the database.
        ///  [Required]
        /// </summary>
        ItemTree DeclaredMembers { get; }

        /// <summary>
        ///  Index provides a map from strings to the DeclaredMembers index of members
        ///  with that name. [Required]
        /// </summary>
        MemberIndex Index { get; }

        // Optional details which may be exposed by DeclaredMembers.

        String8 GetPackageName(int memberIndex);
        int GetPopularityRank(int memberIndex);
        String8 GetPackageVersion(int memberIndex);
        String8 GetAssemblyName(int memberIndex);
        String8 GetFrameworkTargets(int memberIndex);
        SymbolType GetMemberType(int memberIndex);
        SymbolModifier GetMemberModifiers(int memberIndex);
        String8 GetMemberParameters(int memberIndex);
        Path8 GetMemberFilePath(int memberIndex);
        ushort GetMemberFileLine(int memberIndex);
        ushort GetMemberCharInLine(int memberIndex);

        void SetLocation(int memberIndex, string filePath, ushort line, ushort charInLine);
    }

    public static class MemberDatabase
    {
        public static IMemberDatabase Load(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".idx":
                    PackageDatabase db = new PackageDatabase();
                    db.FileRead(filePath);
                    return db;
                case ".ardb":
                    AddReferenceDatabase ardb = new AddReferenceDatabase(ArdbVersion.Current);
                    ardb.FileRead(filePath);
                    return ardb;
                default:
                    throw new NotSupportedException(String.Format(Resources.MemberDatabase_ExtensionNotSupported, extension));
            }
        }

        public static bool TryGetAncestorOfType(this IMemberDatabase db, int memberIndex, SymbolType ancestorType, out int ancestor)
        {
            ancestor = memberIndex;
            while (ancestor > 0)
            {
                if (db.GetMemberType(ancestor) == ancestorType) { return true; }
                ancestor = db.DeclaredMembers.GetParent(ancestor);
            }
            return false;
        }

        public static bool TryGetFirstChildOfType(this IMemberDatabase db, int memberIndex, SymbolType symbolType, out int childIndex)
        {
            childIndex = db.DeclaredMembers.GetFirstChild(memberIndex);
            while (childIndex > 0)
            {
                if (db.GetMemberType(childIndex) == symbolType) { return true; }
                childIndex = db.DeclaredMembers.GetNextSibling(childIndex);
            }
            return false;
        }
    }
}
