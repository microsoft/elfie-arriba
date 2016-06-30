// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public struct Symbol
    {
        private IMemberDatabase _database;

        public int Index { get; private set; }

        public Symbol(IMemberDatabase database, int index)
        {
            _database = database;
            Index = index;
        }

        /// <summary>
        ///  Returns whether this Symbol instance is valid. After calling
        ///  Parent, FirstChild, or NextSibling, check IsValid to determine
        ///  if something was returned.
        /// </summary>
        public bool IsValid
        {
            get { return _database != null; }
        }

        #region Basic Properties
        /// <summary>
        ///  Returns the name of this member [Parse]
        /// </summary>
        public String8 Name
        {
            get { return _database.StringStore[_database.DeclaredMembers.GetNameIdentifier(Index)]; }
        }

        /// <summary>
        ///  Returns the type of this member [SymbolType.Method]
        /// </summary>
        public SymbolType Type
        {
            get { return _database.GetMemberType(Index); }
        }

        /// <summary>
        ///  Returns modifiers of this member [SymbolModifier.Public | SymbolModifier.Static]
        /// </summary>
        public SymbolModifier Modifiers
        {
            get { return _database.GetMemberModifiers(Index); }
        }

        /// <summary>
        ///  Returns the parameters (signature) of this member, if any [string, IFormatProvider]
        /// </summary>
        public String8 Parameters
        {
            get { return _database.GetMemberParameters(Index); }
        }

        /// <summary>
        ///  Returns the PackageName, if any, containing this member [mscorlib]
        /// </summary>
        public String8 PackageName
        {
            get { return _database.GetPackageName(Index); }
        }

        /// <summary>
        ///  Returns the AssemblyName containing this member [mscorlib] or [mscorlib.dll],
        ///  depending on Roslyn Assembly.Identity.Name returned.
        /// </summary>
        public String8 AssemblyName
        {
            get { return _database.GetAssemblyName(Index); }
        }

        public String8 FrameworkTargets
        {
            get { return _database.GetFrameworkTargets(Index); }
        }

        /// <summary>
        ///  Returns the AssemblyName containing this member [mscorlib] with no file extension.
        /// </summary>
        public String8 AssemblyNameWithoutExtension
        {
            get
            {
                String8 assemblyName = this.AssemblyName;
                int lastDot = assemblyName.LastIndexOf((byte)'.');
                if (lastDot == -1) return assemblyName;

                String8 extension = assemblyName.Substring(lastDot);
                if (extension.CompareTo(".exe", true) == 0 || extension.CompareTo(".dll", true) == 0 || extension.CompareTo(".winmd", true) == 0)
                {
                    return assemblyName.Substring(0, lastDot);
                }
                else
                {
                    return assemblyName;
                }
            }
        }

        /// <summary>
        ///  Returns the Extended Type [IEnumerable&lt;T&gt;] for an extension method [OrderBy].
        ///  Returns String8.Empty for non-extension methods."
        /// </summary>
        public String8 ExtendedType
        {
            get
            {
                // If not an extension method, return empty
                if (this.Type != SymbolType.ExtensionMethod) return String8.Empty;

                int extendedTypeIndex = _database.DeclaredMembers.GetFirstChild(Index);

                // If no first child, return empty
                if (extendedTypeIndex < 0) return String8.Empty;

                // If child not extended type, return empty
                if (_database.GetMemberType(extendedTypeIndex) != SymbolType.ExtendedType) return String8.Empty;

                // Otherwise, return extended type full name [stored as name of extended type node]
                return _database.StringStore[_database.DeclaredMembers.GetNameIdentifier(extendedTypeIndex)];
            }
        }

        /// <summary>
        ///  Returns the full namespace and name of this member [System.DateTime.Parse]
        /// </summary>
        public Path8 FullName
        {
            get
            {
                // Return the Path including the full namespace
                return new Path8(_database.StringStore, _database.DeclaredMembers, Index, '.', this.DepthToRootNamespace);
            }
        }

        /// <summary>
        ///  Returns the full name of this member above the name itself. For classes, this is the namespace.
        ///  For methods, the class name. [System.DateTime]
        /// </summary>
        public Path8 ContainerName
        {
            get
            {
                int namespaceIndex = _database.DeclaredMembers.GetParent(Index);
                return new Path8(_database.StringStore, _database.DeclaredMembers, namespaceIndex, '.', this.DepthToRootNamespace - 1);
            }
        }

        private int DepthToRootNamespace
        {
            get
            {
                int depth = 0;

                int current = Index;
                while (current > 0)
                {
                    SymbolType currentType = _database.GetMemberType(current);
                    if (currentType.IsAboveNamespace()) break;

                    current = _database.DeclaredMembers.GetParent(current);
                    depth++;
                }

                return depth;
            }
        }
        #endregion

        #region Location
        /// <summary>
        ///  Returns whether the location is known for this member.
        /// </summary>
        public bool HasLocation
        {
            get { return _database.GetMemberFileLine(Index) > 0; }
        }

        /// <summary>
        ///  Returns the file path (or URL) of the file containing this member, if known, or String8.Empty
        /// </summary>
        public Path8 FilePath
        {
            get { return _database.GetMemberFilePath(Index); }
        }

        /// <summary>
        ///  Returns the line containing the declaration of this member, or 0 if unknown.
        /// </summary>
        public ushort Line
        {
            get { return _database.GetMemberFileLine(Index); }
        }

        /// <summary>
        ///  Returns the character of the start of the declaration of this member, or 0 if unknown.
        /// </summary>
        public ushort CharInLine
        {
            get { return _database.GetMemberCharInLine(Index); }
        }
        #endregion

        #region Traversal
        /// <summary>
        ///  Get the Parent of this Symbol, if any.
        /// </summary>
        /// <returns>Parent symbol or symbol with IsValid = false if already at root</returns>
        public Symbol Parent()
        {
            int parentIndex = _database.DeclaredMembers.GetParent(Index);
            if (parentIndex > 0) return new Symbol(_database, parentIndex);
            return default(Symbol);
        }

        /// <summary>
        ///  Get the first child of this Symbol, if any.
        /// </summary>
        /// <returns>First child symbol or symbol with IsValid = false if no children</returns>
        public Symbol FirstChild()
        {
            int childIndex = _database.DeclaredMembers.GetFirstChild(Index);
            if (childIndex > 0) return new Symbol(_database, childIndex);
            return default(Symbol);
        }

        /// <summary>
        ///  Get the next sibling of this Symbol, if any.
        /// </summary>
        /// <returns>Next sibling symbol or symbol with IsValid = false if no more siblings</returns>
        public Symbol NextSibling()
        {
            int siblingIndex = _database.DeclaredMembers.GetNextSibling(Index);
            if (siblingIndex > 0) return new Symbol(_database, siblingIndex);
            return default(Symbol);
        }

        /// <summary>
        ///  Return the ancestor in the hierarchy above this symbol of the desired type.
        ///  Can be used to find properties like assembly and package for future
        ///  SymbolTypes which don't have an explicit Symbol API added.
        ///  
        ///  Throws if an ancestor of the desired type is not found.
        /// </summary>
        /// <param name="type">SymbolType to look for above this Symbol</param>
        /// <returns>Symbol for the ancestor of this Symbol of the desired type.</returns>
        public Symbol GetAncestorOfType(SymbolType type)
        {
            int ancestor;

            if (!_database.TryGetAncestorOfType(Index, type, out ancestor))
            {
                throw new ArgumentOutOfRangeException(String.Format(Resources.Database_NoAncestorOfType, type.ToString(), new Symbol(_database, Index).FullName.ToString()));
            }
            return new Symbol(_database, ancestor);
        }

        /// <summary>
        ///  Walk the tree of this Symbol and descendants, calling the action on each of them.
        /// </summary>
        /// <param name="action">Action to invoke for each symbol</param>
        public void Walk(Action<Symbol> action)
        {
            // Call the action for this node (except the sentinel root)
            if (Index != 0) action(this);

            // Recurse on children
            Symbol child = this.FirstChild();
            while (child.IsValid)
            {
                child.Walk(action);
                child = child.NextSibling();
            }
        }
        #endregion

        #region Tree Logging
        /// <summary>
        ///  Write this Symbol [type, name, signature, location] to the given TextWriter.
        ///  This does not write a following newline.
        /// </summary>
        /// <param name="writer">TextWriter to write to</param>
        public void Write(TextWriter writer)
        {
            // Write type (if available)
            SymbolType type = this.Type;
            if (this.Type != SymbolType.Any)
            {
                writer.Write(this.Type.ToString());
                writer.Write(" ");
            }

            // Write the name and parameters
            WriteSignature(writer);

            // Write the location (if available)
            if (this.HasLocation)
            {
                writer.Write(" @");
                WriteLocation(writer);
            }
        }

        /// <summary>
        ///  Write this Symbol [name, type, modifiers, signature, location] to the given TextWriter.
        ///  Values are tab-delimited.
        ///  This does not write a following newline.
        /// </summary>
        /// <param name="writer">TextWriter to write to</param>
        public void WriteDetails(TextWriter writer)
        {
            SymbolType type = this.Type;

            WriteSignature(writer);
            writer.Write('\t');
            writer.Write(type);
            writer.Write('\t');
            writer.Write(this.Modifiers);

            if (this.HasLocation)
            {
                writer.Write("\t@");
                WriteLocation(writer);
            }
        }

        public int WriteSignature(TextWriter writer)
        {
            int lengthWritten = 0;

            // Write name
            lengthWritten += this.Name.WriteTo(writer);

            // Write parameters (if available)
            if (!this.Parameters.IsEmpty())
            {
                SymbolType type = this.Type;
                bool useBrace = type.IsBracedType();
                writer.Write(useBrace ? "[" : "(");
                lengthWritten += this.Parameters.WriteTo(writer);
                writer.Write(useBrace ? "]" : ")");

                lengthWritten += 2; // Braces/Parens
            }

            return lengthWritten;
        }

        /// <summary>
        ///  Write the location, if known, of this symbol in (Line, CharInLine) format.
        /// </summary>
        /// <param name="writer">TextWriter to </param>
        public void WriteLocation(TextWriter writer)
        {
            // Write location (if found)
            if (this.HasLocation)
            {
                this.FilePath.WriteTo(writer);

                writer.Write("(");
                writer.Write(this.Line);
                writer.Write(",");
                writer.Write(this.CharInLine);
                writer.Write(")");
            }
        }

        /// <summary>
        ///  Write an indented tree including this Symbol and descendants to the given writer.
        ///  Each Symbol is written with Symbol.WriteDetails.
        /// </summary>
        /// <param name="writer">TextWriter to write to</param>
        /// <param name="indent">Indent to start with (0 for no indent)</param>
        public void WriteTree(TextWriter writer, int indent)
        {
            for (int i = 0; i < indent; ++i)
            {
                writer.Write('\t');
            }

            WriteDetails(writer);
            writer.WriteLine();

            Symbol child = this.FirstChild();
            while (child.IsValid)
            {
                child.WriteTree(writer, indent + 1);
                child = child.NextSibling();
            }
        }
        #endregion
    }
}
