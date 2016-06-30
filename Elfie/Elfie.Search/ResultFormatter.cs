// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;

namespace Microsoft.CodeAnalysis.Elfie.Search
{
    /// <summary>
    ///  ResultFormatter writes a PartialArray&lt;Symbol&gt; in a number of different formats.
    /// </summary>
    internal static class ResultFormatter
    {
        public static void WriteMatchesInFlatFormat(TextWriter writer, PartialArray<Symbol> results)
        {
            for (int i = 0; i < results.Count; ++i)
            {
                Symbol result = results[i];
                Path8 containerName = result.ContainerName;
                if (!containerName.IsEmpty)
                {
                    containerName.WriteTo(writer);
                    writer.Write('.');
                }

                result.WriteSignature(writer);
                writer.Write('\t');

                // Write location (local/found code) or package (package index)
                if (result.HasLocation)
                {
                    result.FilePath.Name.WriteTo(writer);
                    writer.Write('(');
                    writer.Write(result.Line);
                    writer.Write(')');
                }
                else
                {
                    result.PackageName.WriteTo(writer);
                }

                writer.WriteLine();
            }
        }

        public static void WriteMatchesForToolUse(TextWriter writer, PartialArray<Symbol> results)
        {
            for (int i = 0; i < results.Count; ++i)
            {
                Symbol result = results[i];
                Path8 containerName = result.ContainerName;
                if (!containerName.IsEmpty)
                {
                    containerName.WriteTo(writer);
                    writer.Write('.');
                }

                result.WriteSignature(writer);
                writer.Write('\t');

                // Write full location (local/found code) or package (package index)
                if (result.HasLocation)
                {
                    result.FilePath.WriteTo(writer);
                    writer.Write('(');
                    writer.Write(result.Line);
                    writer.Write(')');
                }
                else
                {
                    result.PackageName.WriteTo(writer);
                }

                writer.WriteLine();
            }
        }

        public static void WriteMatchesInTreeFormat(TextWriter w, PartialArray<Symbol> results, IMemberDatabase db)
        {
            // Mark every result and every ancestor to draw
            bool[] nodesToDraw = new bool[db.Count];
            for (int i = 0; i < results.Count; ++i)
            {
                Symbol result = results[i];

                while (result.IsValid)
                {
                    nodesToDraw[result.Index] = true;
                    result = result.Parent();
                }
            }

            // Draw the results of interest [assemblies and down only]
            WriteTextTree(w, db, 0, -2, nodesToDraw);
        }

        private static void WriteTextTree(TextWriter w, IMemberDatabase db, int index, int indent, bool[] nodesToDraw)
        {
            if (indent >= 0)
            {
                if (nodesToDraw[index] == false) return;

                for (int i = 0; i < indent; ++i)
                {
                    w.Write("\t");
                }

                w.Write((char)db.GetMemberType(index));
                w.Write(" ");
                db.StringStore[db.DeclaredMembers.GetNameIdentifier(index)].WriteTo(w);
                w.WriteLine();
            }

            int child = db.DeclaredMembers.GetFirstChild(index);
            while (child > 0)
            {
                WriteTextTree(w, db, child, indent + 1, nodesToDraw);
                child = db.DeclaredMembers.GetNextSibling(child);
            }
        }

        public static void WriteMatchesIndentedUnderContainers(TextWriter writer, PartialArray<Symbol> results)
        {
            Symbol lastContainerType = default(Symbol);

            for (int i = 0; i < results.Count; ++i)
            {
                Symbol result = results[i];

                Symbol containingType = result;
                while (containingType.IsValid && !containingType.Type.IsType() && !containingType.Type.IsAboveType())
                {
                    containingType = containingType.Parent();
                }

                if (lastContainerType.Index != containingType.Index)
                {
                    lastContainerType = containingType;
                    if (containingType.IsValid) WriteContainerHeading(writer, containingType);
                }

                writer.Write("  ");

                if (result.HasLocation)
                {
                    writer.Write("L");
                    writer.Write(result.Line);
                    writer.Write('\t');
                }

                result.WriteSignature(writer);

                writer.WriteLine();
            }
        }

        private static void WriteContainerHeading(TextWriter writer, Symbol result)
        {
            if (result.HasLocation)
            {
                result.FilePath.Name.WriteTo(writer);
                writer.Write('(');
                writer.Write(result.Line);
                writer.Write(')');
            }
            else
            {
                result.PackageName.WriteTo(writer);
            }

            writer.Write('\t');

            Path8 containerName = result.ContainerName;
            if (!containerName.IsEmpty)
            {
                containerName.WriteTo(writer);
                writer.Write('.');
            }

            result.WriteSignature(writer);

            writer.WriteLine();
        }
    }
}
