// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.EndToEnd
{
    public class IndexAndSearchReferenceAssemblies
    {
        public bool Run(TextWriter writer)
        {
            bool result = true;
            string testDataPath = Path.Combine(Environment.CurrentDirectory, @"..\..\TestData\PackageDatabases");
            string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string[] dirs = new string[] { "v4.0", "v4.5.2" };
            string[] files = new string[] { "System.Data.dll" };

            rootPath = Path.Combine(rootPath, @"Reference Assemblies\Microsoft\Framework\.NETFramework\");

            foreach (string dir in dirs)
            {
                foreach (string file in files)
                {
                    HashSet<string> publicNonNested = new HashSet<string>();
                    HashSet<string> otherTypes = new HashSet<string>();
                    string assemblyPath = Path.Combine(rootPath, dir, file);
                    Walk(assemblyPath, publicNonNested, otherTypes);

                    PackageDatabase db = Indexer.IndexCommand.Index(assemblyPath, true, true);
                    File.WriteAllText(Path.Combine(testDataPath, file + "." + dir + ".actual.txt"), db.ToString());
                    db.ConvertToImmutable();


                    var results = new PartialArray<Symbol>(10);

                    foreach (string typeName in publicNonNested)
                    {
                        var query = new MemberQuery(typeName, false, false);
                        query.TryFindMembers(db, ref results);

                        if (results.Count == 0)
                        {
                            writer.WriteLine("Found " + results.Count + " (instead of 1 or more) matches for " + typeName);
                            result = false;
                        }
                    }

                    foreach (string typeName in otherTypes)
                    {
                        var query = new MemberQuery(typeName, true, true);
                        query.TryFindMembers(db, ref results);

                        if (results.Count == 0)
                        {
                            writer.WriteLine("Found " + results.Count + " (instead of 1 or more) matches for " + typeName);
                            result = false;
                        }
                    }
                }
            }
            return result;
        }

        //{
        //    string targetFramework = Path.GetFileName(targetFrameworkPath);
        //    string binaryPath = Path.Combine(targetFrameworkPath, "Newtonsoft.Json.dll");

        //    PackageDatabase db = Indexer.IndexCommand.Index(binaryPath, true);
        //    AddReferenceDatabase ardb = new AddReferenceDatabase();
        //    ardb.AddReferenceAssemblyTypes(db);
        //    Write.ToFile(ardb.WriteText, binaryPath + ".ardb.txt");
        //}


        public void Walk(string binaryPath, HashSet<string> publicNonNestedTypes, HashSet<string> otherTypes)
        {
            FileStream stream = new FileStream(binaryPath, FileMode.Open, FileAccess.Read);

            // NOTE: Need to keep PEReader alive through crawl to avoid AV in looking up signatures
            using (PEReader peReader = new PEReader(stream))
            {
                if (peReader.HasMetadata == false) return;

                Trace.WriteLine("\t" + binaryPath);

                MetadataReader mdReader = peReader.GetMetadataReader();
                foreach (TypeDefinitionHandle typeHandle in mdReader.TypeDefinitions)
                {
                    TypeDefinition type = mdReader.GetTypeDefinition(typeHandle);

                    string namespaceString = mdReader.GetString(type.Namespace);

                    // Get the name and remove generic suffix (List`1 => List)
                    string metadataName = mdReader.GetString(type.Name);
                    int genericNameSuffixIndex = metadataName.IndexOf('`');
                    string baseName = (genericNameSuffixIndex < 0 ? metadataName : metadataName.Substring(0, genericNameSuffixIndex));
                    //                    string fqn = type.Namespace + "." + baseName;
                    string fqn = baseName;

                    if ((type.Attributes & TypeAttributes.Public) == TypeAttributes.Public)
                    {
                        publicNonNestedTypes.Add(fqn);
                    }
                    else
                    {
                        otherTypes.Add(fqn);
                    }
                    WalkType(mdReader, typeHandle);
                }
            }
        }

        private void WalkType(MetadataReader mdReader, TypeDefinitionHandle handle)
        {
            TypeDefinition type = mdReader.GetTypeDefinition(handle);
            var attributes = type.Attributes;

            foreach (TypeDefinitionHandle nestedTypeHandle in type.GetNestedTypes())
            {
                WalkType(mdReader, nestedTypeHandle);
            }
        }
    }
}