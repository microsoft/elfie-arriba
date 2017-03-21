// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    public class SrmCrawler : ICrawler
    {
        public bool IncludeSignatures { get; set; }
        public bool IncludeMembers { get; set; }
        public bool IncludeNonPublicMembers { get; set; }
        public bool IncludeCodeLocations { get; set; }

        private PdbSymbolProvider PDB { get; set; }

        public SrmCrawler()
        {
            this.IncludeSignatures = true;
            this.IncludeMembers = true;
            this.IncludeNonPublicMembers = true;
            this.IncludeCodeLocations = true;
        }

        public void Walk(string binaryPath, MutableSymbol parent)
        {
            if (!FileIO.IsManagedBinary(binaryPath)) throw new ArgumentException(String.Format("SrmCrawler doesn't know how to walk file with extension '{0}'", Path.GetExtension(binaryPath)));

            FileStream stream = new FileStream(binaryPath, FileMode.Open, FileAccess.Read);

            // NOTE: Need to keep PEReader alive through crawl to avoid AV in looking up signatures
            using (PEReader peReader = new PEReader(stream))
            {
                if (peReader.HasMetadata == false) return;

                Trace.WriteLine("\t" + binaryPath);

                using (PdbSymbolProvider pdbProvider = PdbSymbolProvider.TryBuildProvider(binaryPath))
                {
                    PDB = pdbProvider;

                    MutableSymbol assemblyRoot = parent.AddChild(new MutableSymbol(Path.GetFileName(binaryPath), SymbolType.Assembly));

                    // Walk all non-nested types. Namespaces are derived as found. Nested types will be found during crawl of containing type.
                    MetadataReader mdReader = peReader.GetMetadataReader();
                    foreach (TypeDefinitionHandle typeHandle in mdReader.TypeDefinitions)
                    {
                        TypeDefinition type = mdReader.GetTypeDefinition(typeHandle);
                        string namespaceString = mdReader.GetString(type.Namespace);

                        if (!type.Attributes.IsNested())
                        {
                            MutableSymbol ns = assemblyRoot.FindOrAddPath(namespaceString, '.', SymbolType.Namespace);
                            WalkType(mdReader, typeHandle, ns);
                        }
                    }
                }
            }
        }

        private void WalkType(MetadataReader mdReader, TypeDefinitionHandle handle, MutableSymbol parent)
        {
            TypeDefinition type = mdReader.GetTypeDefinition(handle);
            var attributes = type.Attributes;

            // Get the name and remove generic suffix (List`1 => List)
            string metadataName = mdReader.GetString(type.Name);
            int genericNameSuffixIndex = metadataName.IndexOf('`');
            string baseName = (genericNameSuffixIndex < 0 ? metadataName : metadataName.Substring(0, genericNameSuffixIndex));

            MutableSymbol result = new MutableSymbol(baseName, GetTypeType(mdReader, type));
            AddModifiers(attributes, result);
            AddLocation(handle, result);

            if (IsExcluded(result)) return;
            parent.AddChild(result);

            foreach (TypeDefinitionHandle nestedTypeHandle in type.GetNestedTypes())
            {
                WalkType(mdReader, nestedTypeHandle, result);
            }

            if (this.IncludeMembers)
            {
                foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
                {
                    WalkMethod(mdReader, methodHandle, result);
                }

                foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
                {
                    WalkField(mdReader, fieldHandle, result);
                }

                foreach (PropertyDefinitionHandle propertyHandle in type.GetProperties())
                {
                    WalkProperty(mdReader, propertyHandle, result);
                }

                // NOTE: type.GetEvents, type.GetInterfaceImplementations are not converted.
            }
        }

        private void WalkMethod(MetadataReader mdReader, MethodDefinitionHandle handle, MutableSymbol parent)
        {
            MethodDefinition method = mdReader.GetMethodDefinition(handle);
            MethodAttributes attributes = method.Attributes;
            string name = mdReader.GetString(method.Name);

            MutableSymbol result = new MutableSymbol(name, GetMethodType(attributes, name));
            AddModifiers(attributes, result);
            AddLocation(handle, result);
            AddParameters(mdReader, method, result);

            // Make Constructor/Destructors use the type name as the method name [like Ctrl+, search]
            if (result.Type == SymbolType.Constructor || result.Type == SymbolType.StaticConstructor || result.Type == SymbolType.Destructor)
            {
                result.Name = parent.Name.ToString();
            }

            if (IsExcluded(result)) return;

            parent.AddChild(result);

            // TODO: Identify Extension Methods. Issue: Requires a lot of Blob reading. See http://source.roslyn.io/#q=PEMethodSymbol.IsExtensionMethod, http://source.roslyn.io/#q=PEModule.HasExtensionAttribute
        }

        private void WalkField(MetadataReader mdReader, FieldDefinitionHandle handle, MutableSymbol parent)
        {
            FieldDefinition field = mdReader.GetFieldDefinition(handle);

            MutableSymbol result = new MutableSymbol(mdReader.GetString(field.Name), SymbolType.Field);
            AddModifiers(field.Attributes, result);
            AddLocation(handle, result);

            if (IsExcluded(result)) return;
            parent.AddChild(result);
        }

        private void WalkProperty(MetadataReader mdReader, PropertyDefinitionHandle handle, MutableSymbol parent)
        {
            PropertyDefinition prop = mdReader.GetPropertyDefinition(handle);

            MutableSymbol result = new MutableSymbol(mdReader.GetString(prop.Name), SymbolType.Property);

            // Use the accessibility and location of the getter [or setter, if write only property]
            // Not identical to Roslyn PEPropertyDeclaration but much simpler
            MethodDefinitionHandle getterOrSetterHandle = prop.GetAccessors().Getter;
            if (getterOrSetterHandle.IsNil) getterOrSetterHandle = prop.GetAccessors().Setter;

            // If we couldn't retrieve a getter or setter, exclude this property
            if (getterOrSetterHandle.IsNil) return;

            MethodDefinition getterOrSetter = mdReader.GetMethodDefinition(getterOrSetterHandle);
            AddModifiers(getterOrSetter.Attributes, result);
            AddLocation(getterOrSetterHandle, result);
            AddParameters(mdReader, getterOrSetter, result);

            // If this is an Indexer, rename it and retype it
            // Roslyn PEPropertySymbol.IsIndexer is also just based on the name.
            if (result.Name == "Item")
            {
                result.Name = "this[]";
                result.Type = SymbolType.Indexer;
            }

            if (IsExcluded(result)) return;
            parent.AddChild(result);
        }

        private SymbolType GetTypeType(MetadataReader mdReader, TypeDefinition type)
        {
            if (type.Attributes.HasFlag(TypeAttributes.Interface)) return SymbolType.Interface;

            string baseTypeName = GetBaseTypeName(mdReader, type);

            if (baseTypeName.Equals("System.Enum")) return SymbolType.Enum;
            if (baseTypeName.Equals("System.ValueType")) return SymbolType.Struct;

            return SymbolType.Class;
        }

        private string GetBaseTypeName(MetadataReader mdReader, TypeDefinition type)
        {
            EntityHandle baseTypeHandle = type.BaseType;
            if (baseTypeHandle.IsNil) return String.Empty;

            HandleKind baseTypeHandleKind = baseTypeHandle.Kind;

            if (baseTypeHandleKind == HandleKind.TypeDefinition)
            {
                TypeDefinition baseType = mdReader.GetTypeDefinition((TypeDefinitionHandle)baseTypeHandle);
                return mdReader.GetString(baseType.Namespace) + "." + mdReader.GetString(baseType.Name);
            }
            else if (baseTypeHandleKind == HandleKind.TypeSpecification)
            {
                TypeSpecification baseType = mdReader.GetTypeSpecification((TypeSpecificationHandle)baseTypeHandle);

                // Don't have logic to convert signatures to names/strings
                // NOTE: Enums and Structs can't have user defined base types, so this shouldn't break distinguising enums/structs.
                return String.Empty;
            }
            else if (baseTypeHandleKind == HandleKind.TypeReference)
            {
                TypeReference baseType = mdReader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                return mdReader.GetString(baseType.Namespace) + "." + mdReader.GetString(baseType.Name);
            }

            return String.Empty;
        }

        private SymbolType GetMethodType(MethodAttributes attributes, string symbolName)
        {
            // Similar to PEMethodSymbol.ComputeMethodKind
            if (symbolName.Equals(".cctor")) return SymbolType.StaticConstructor;
            if (symbolName.Equals(".ctor")) return SymbolType.Constructor;
            if (symbolName.Equals("Finalize")) return SymbolType.Destructor;

            // Exclude Property Getter/Setters [rolled up as Properties]
            if (symbolName.StartsWith("get_") || symbolName.StartsWith("set_")) return SymbolType.Excluded;

            // Exclude Implicit Casts
            if (symbolName.Equals("op_Implicit")) return SymbolType.Excluded;

            // NOTE: Operators aren't given a distinct type in Elfie
            return SymbolType.Method;
        }

        private void AddParameters(MetadataReader mdReader, MethodDefinition method, MutableSymbol result)
        {
            if (!this.IncludeSignatures) return;

            StringBuilder parameterString = new StringBuilder();

            StringSignatureProvider provider = new StringSignatureProvider(mdReader, mdReader.GetTypeDefinition(method.GetDeclaringType()), method);

            MethodSignature<string> signature = method.DecodeSignature<string, DisassemblingGenericContext>(provider, null);
            foreach (string value in signature.ParameterTypes)
            {
                if (parameterString.Length > 0) parameterString.Append(", ");
                parameterString.Append(value);
            }

            result.Parameters = parameterString.ToString();
        }

        private void AddModifiers(TypeAttributes attributes, MutableSymbol symbolToAdd)
        {
            SymbolModifier modifiers = symbolToAdd.Modifiers;

            // Same as Roslyn PENamedTypeSymbol.DeclaredAccessibility
            switch (attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NestedAssembly:
                    modifiers = SymbolModifier.Internal;
                    break;

                case TypeAttributes.NestedFamORAssem:
                case TypeAttributes.NestedFamANDAssem:
                    modifiers = SymbolModifier.Protected | SymbolModifier.Internal;
                    break;

                case TypeAttributes.NestedPrivate:
                    modifiers = SymbolModifier.Private;
                    break;

                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                    modifiers = SymbolModifier.Public;
                    break;

                case TypeAttributes.NestedFamily:
                    modifiers = SymbolModifier.Protected;
                    break;

                case TypeAttributes.NotPublic:
                    modifiers = SymbolModifier.Internal;
                    break;
            }


            // Same as Roslyn PENamedTypeSymbol.IsStatic
            if (attributes.HasFlag(TypeAttributes.Sealed) && attributes.HasFlag(TypeAttributes.Abstract)) modifiers |= SymbolModifier.Static;

            symbolToAdd.Modifiers = modifiers;
        }

        private void AddModifiers(MethodAttributes attributes, MutableSymbol symbolToAdd)
        {
            SymbolModifier modifiers = symbolToAdd.Modifiers;

            // Same as Roslyn PEMethodSymbol.DeclaredAccessibility
            switch (attributes & MethodAttributes.MemberAccessMask)
            {
                case MethodAttributes.Assembly:
                    modifiers |= SymbolModifier.Internal;
                    break;

                case MethodAttributes.FamORAssem:
                case MethodAttributes.FamANDAssem:
                    modifiers |= SymbolModifier.Protected | SymbolModifier.Internal;
                    break;

                case MethodAttributes.Private:
                case MethodAttributes.PrivateScope:
                    modifiers |= SymbolModifier.Private;
                    break;

                case MethodAttributes.Public:
                    modifiers |= SymbolModifier.Public;
                    break;

                case MethodAttributes.Family:
                    modifiers |= SymbolModifier.Protected;
                    break;
            }

            if (attributes.HasFlag(MethodAttributes.Static)) modifiers |= SymbolModifier.Static;

            symbolToAdd.Modifiers = modifiers;
        }

        private void AddModifiers(FieldAttributes attributes, MutableSymbol symbolToAdd)
        {
            SymbolModifier modifiers = symbolToAdd.Modifiers;

            // Same as Roslyn PEFieldSymbol.DeclaredAccessibility
            switch (attributes & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.Assembly:
                    modifiers = SymbolModifier.Internal;
                    break;

                case FieldAttributes.FamORAssem:
                case FieldAttributes.FamANDAssem:
                    modifiers = SymbolModifier.Protected | SymbolModifier.Internal;
                    break;

                case FieldAttributes.Private:
                case FieldAttributes.PrivateScope:
                    modifiers = SymbolModifier.Private;
                    break;

                case FieldAttributes.Public:
                    modifiers = SymbolModifier.Public;
                    break;

                case FieldAttributes.Family:
                    modifiers = SymbolModifier.Protected;
                    break;
            }

            if (attributes.HasFlag(FieldAttributes.Static)) modifiers |= SymbolModifier.Static;

            symbolToAdd.Modifiers = modifiers;
        }

        private void AddLocation(Handle handle, MutableSymbol symbolToAdd)
        {
            if (!this.IncludeCodeLocations) return;

            if (PDB != null)
            {
                int token = MetadataTokens.GetToken(handle);

                // If found, associate the member with the first location found
                IEnumerable<ILSequencePoint> locations = PDB.GetSequencePointsForMethod(token);
                foreach (ILSequencePoint location in locations)
                {
                    symbolToAdd.FilePath = location.Document;
                    symbolToAdd.Line = location.StartLine.TrimToUShort();
                    symbolToAdd.CharInLine = location.StartCharInLine.TrimToUShort();
                    return;
                }
            }
        }

        private bool IsExcluded(MutableSymbol symbolToAdd)
        {
            // Exclude members we don't want indexed (backing fields, property get/set methods)
            if (symbolToAdd.Type == SymbolType.Excluded)
            {
                return true;
            }

            // Exclude types which are generated under the covers (IEnumerable worker classes)
            if (symbolToAdd.Name.StartsWith("<")) return true;

            // Exclude weird value__ fields appearing on enum types
            if (symbolToAdd.Name.Equals("value__"))
            {
                return true;
            }

            // Exclude constructors for enums [TODO]
            //if (symbolToAdd.Type == SymbolType.Constructor)
            //{
            //    INamedTypeSymbol parentType = symbol.ContainingType.BaseType;
            //    if (parentType != null && parentType.Name.Equals("Enum"))
            //    {
            //        return true;
            //    }
            //}

            // Exclude Event add_ and remove_ methods [rolled up in Event]
            if (symbolToAdd.Type == SymbolType.Method && (symbolToAdd.Name.StartsWith("add_") || symbolToAdd.Name.StartsWith("remove_")))
            {
                if (!this.IncludeSignatures || symbolToAdd.Parameters.Equals("EventHandler"))
                {
                    return true;
                }
            }

            // Exclude non-public members *if configured to*
            if (this.IncludeNonPublicMembers == false)
            {
                if (!symbolToAdd.Modifiers.HasFlag(SymbolModifier.Public))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
