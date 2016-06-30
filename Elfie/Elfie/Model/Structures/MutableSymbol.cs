// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace Microsoft.CodeAnalysis.Elfie.Model.Structures
{
    public class MutableSymbol
    {
        private PackageDatabase _database;
        private int _index;

        public string Name;
        public SymbolType Type;
        public SymbolModifier Modifiers;
        public string Parameters;

        public string FilePath;
        public ushort Line;
        public ushort CharInLine;

        internal MutableSymbol(PackageDatabase database, int index)
        {
            _database = database;
            _index = index;
        }

        public MutableSymbol(string name, SymbolType type)
        {
            this.Name = name;
            this.Type = type;
        }

        public MutableSymbol(Symbol source)
        {
            this.Name = source.Name.ToString();
            this.Type = source.Type;
            this.Modifiers = source.Modifiers;
            this.Parameters = source.Parameters.ToString();
            this.FilePath = source.FilePath.ToString();
            this.Line = source.Line;
            this.CharInLine = source.CharInLine;
        }

        public MutableSymbol FindByFullName(string path, char delimiter)
        {
            String8 path8 = String8.Convert(path, new byte[String8.GetLength(path)]);
            String8Set splitPath8 = path8.Split(delimiter, new int[String8Set.GetLength(path8, delimiter)]);
            return new MutableSymbol(_database, _database.DeclaredMembers.FindByPath(_index, splitPath8, _database.StringStore));
        }

        public MutableSymbol FindOrAddPath(string path, char delimiter, SymbolType pathPartType)
        {
            String8 path8 = String8.Convert(path, new byte[String8.GetLength(path)]);
            String8Set splitPath8 = path8.Split(delimiter, new int[String8Set.GetLength(path8, delimiter)]);

            int currentIndex = _index;
            for (int i = 0; i < splitPath8.Count; ++i)
            {
                String8 part = splitPath8[i];
                int partNameIdentifier = _database.StringStore.FindOrAddString(part);

                int foundNode;
                if (!_database.DeclaredMembers.TryFindChildByName(currentIndex, partNameIdentifier, out foundNode))
                {
                    foundNode = _database.DeclaredMembers.Add(currentIndex, partNameIdentifier);
                    _database.DeclaredMemberDetails.Add(new SymbolDetails() { Type = pathPartType });
                    _database.DeclaredMemberLocations.Add(default(SymbolLocation));
                }

                currentIndex = foundNode;
            }

            return new MutableSymbol(_database, currentIndex);
        }

        public MutableSymbol AddChild(MutableSymbol child)
        {
            if (_database == null) throw new InvalidOperationException("Can't add a child of a MutableSymbol if the parent hasn't been added to a database yet.");

            // Add the name
            int nameIdentifier = _database.StringStore.FindOrAddString(child.Name);

            // Build the details
            SymbolDetails newDetails = new SymbolDetails();
            newDetails.Type = child.Type;
            newDetails.Modifiers = child.Modifiers;
            newDetails.ParametersIdentifier = _database.StringStore.FindOrAddString(child.Parameters);

            // If the parent has exactly the same item already, just return that one
            for (int i = _database.DeclaredMembers.GetFirstChild(_index); i > 0; i = _database.DeclaredMembers.GetNextSibling(i))
            {
                if (nameIdentifier == _database.DeclaredMembers.GetNameIdentifier(i))
                {
                    SymbolDetails iDetails = _database.DeclaredMemberDetails[i];
                    if (newDetails.Equals(iDetails))
                    {
                        child._database = _database;
                        child._index = i;
                        return child;
                    }
                }
            }

            // Add the symbol itself to the DefinedSymbolTree
            int newSymbolIndex = _database.DeclaredMembers.Add(_index, nameIdentifier);

            // Index this name with this ID
            _database.Index.AddItem(nameIdentifier, newSymbolIndex);

            // Add parallel SymbolDetails
            if (_database.DeclaredMemberDetails.Count != newSymbolIndex) throw new InvalidOperationException(String.Format(Resources.DatabaseArraysOutOfSync, "DeclaredMemberDetails"));
            _database.DeclaredMemberDetails.Add(newDetails);

            // Add parallel location details
            if (String.Equals(this.FilePath, child.FilePath))
            {
                _database.DeclaredMemberLocations.Add(new SymbolLocation(_database.DeclaredMemberLocations[_index].FileIndex, child.Line, child.CharInLine));
            }
            else
            {
                ((IMemberDatabase)_database).SetLocation(newSymbolIndex, child.FilePath, child.Line, child.CharInLine);
            }

            // Bind the child to the database
            child._database = _database;
            child._index = newSymbolIndex;

            return child;
        }

        public void AddTree(Symbol root)
        {
            MutableSymbol newRoot = AddChild(new MutableSymbol(root));

            Symbol child = root.FirstChild();
            while (child.IsValid)
            {
                newRoot.AddTree(child);
                child = child.NextSibling();
            }
        }

        public string ToLocationString()
        {
            return string.Format("{0}({1:n0},{2:n0})", this.FilePath, this.Line, this.CharInLine);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (this.Modifiers != SymbolModifier.None)
            {
                result.Append(this.Modifiers.ToString());
                result.Append(" ");
            }

            result.Append(this.Name);

            result.Append("(");
            result.Append(this.Parameters);
            result.Append(")");

            if (!string.IsNullOrEmpty(this.FilePath))
            {
                result.Append("\r\n\t@");
                result.Append(ToLocationString());
            }

            return result.ToString();
        }
    }
}
