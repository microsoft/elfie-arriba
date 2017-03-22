// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace Microsoft.CodeAnalysis.Elfie.Indexer.Crawlers
{
    public class DisassemblingGenericContext
    {
        public DisassemblingGenericContext(ImmutableArray<string> typeParameters, ImmutableArray<string> methodParameters)
        {
            MethodParameters = methodParameters;
            TypeParameters = typeParameters;
        }

        public ImmutableArray<string> MethodParameters { get; }
        public ImmutableArray<string> TypeParameters { get; }
    }

    /// <summary>
    ///  StringSignatureProvider converts type names and signatures into a canonical
    ///  string form almost identical to Roslyn's MinimallyQualifiedFormat.
    ///  
    ///  The logic here is very similar to src/System.Reflection.Metadata/tests/Metadata/Decoding/DisassemblingTypeProvider.cs,
    ///  modified to conform to the Roslyn MinimallyQualifiedFormat.
    /// </summary>
    public class StringSignatureProvider : ISignatureTypeProvider<string, DisassemblingGenericContext>
    {
        public MetadataReader mdReader { get; set; }
        public TypeDefinition ContainingType { get; set; }
        public MethodDefinition ContainingMethod { get; set; }

        public bool IncludeNamespaceInTypeNames { get; set; }
        public bool IncludeAssemblyNameForReferences { get; set; }
        public bool IncludeNestedTypeContaininingTypeName { get; set; }

        public StringSignatureProvider(MetadataReader mdReader, TypeDefinition containingType, MethodDefinition containingMethod)
        {
            this.mdReader = mdReader;
            this.ContainingType = containingType;
            this.ContainingMethod = containingMethod;
        }

        public virtual string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean:
                    return "bool";

                case PrimitiveTypeCode.Byte:
                    return "byte";

                case PrimitiveTypeCode.Char:
                    return "char";

                case PrimitiveTypeCode.Double:
                    return "double";

                case PrimitiveTypeCode.Int16:
                    return "short";

                case PrimitiveTypeCode.Int32:
                    return "int";

                case PrimitiveTypeCode.Int64:
                    return "long";

                case PrimitiveTypeCode.IntPtr:
                    return "IntPtr";

                case PrimitiveTypeCode.Object:
                    return "object";

                case PrimitiveTypeCode.SByte:
                    return "sbyte";

                case PrimitiveTypeCode.Single:
                    return "float";

                case PrimitiveTypeCode.String:
                    return "string";

                case PrimitiveTypeCode.TypedReference:
                    return "typedref";

                case PrimitiveTypeCode.UInt16:
                    return "ushort";

                case PrimitiveTypeCode.UInt32:
                    return "uint";

                case PrimitiveTypeCode.UInt64:
                    return "ulong";

                case PrimitiveTypeCode.UIntPtr:
                    return "native uint";

                case PrimitiveTypeCode.Void:
                    return "void";

                default:
                    Debug.Assert(false);
                    throw new ArgumentOutOfRangeException("typeCode");
            }
        }

        public virtual string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
        {
            TypeDefinition definition = reader.GetTypeDefinition(handle);

            string name = (!this.IncludeNamespaceInTypeNames || definition.Namespace.IsNil)
                ? RemoveGenericNameSuffix(reader.GetString(definition.Name))
                : reader.GetString(definition.Namespace) + "." + RemoveGenericNameSuffix(reader.GetString(definition.Name));

            if (this.IncludeNestedTypeContaininingTypeName && definition.Attributes.IsNested())
            {
                TypeDefinitionHandle declaringTypeHandle = definition.GetDeclaringType();
                return GetTypeFromDefinition(reader, declaringTypeHandle, 0) + "." + name;
            }

            return name;
        }

        public virtual string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
        {
            TypeReference reference = reader.GetTypeReference(handle);
            Handle scope = reference.ResolutionScope;

            string name = (!this.IncludeNamespaceInTypeNames || reference.Namespace.IsNil)
                ? RemoveGenericNameSuffix(reader.GetString(reference.Name))
                : reader.GetString(reference.Namespace) + "." + RemoveGenericNameSuffix(reader.GetString(reference.Name));

            // If the user has requested no external assembly names, return the type name only
            if (!this.IncludeAssemblyNameForReferences) return name;

            switch (scope.Kind)
            {
                case HandleKind.ModuleReference:
                    return "[.module  " + reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)scope).Name) + "]" + name;

                case HandleKind.AssemblyReference:
                    var assemblyReferenceHandle = (AssemblyReferenceHandle)scope;
                    var assemblyReference = reader.GetAssemblyReference(assemblyReferenceHandle);
                    return "[" + reader.GetString(assemblyReference.Name) + "]" + name;

                case HandleKind.TypeReference:
                    return GetTypeFromReference(reader, (TypeReferenceHandle)scope, rawTypeKind) + "/" + name;

                default:
                    // rare cases:  ModuleDefinition means search within defs of current module (used by WinMDs for projections)
                    //              nil means search exported types of same module (haven't seen this in practice). For the test
                    //              purposes here, it's sufficient to format both like defs.
                    Debug.Assert(scope == Handle.ModuleDefinition || scope.IsNil);
                    return name;
            }
        }

        public virtual string GetTypeFromSpecification(MetadataReader reader, DisassemblingGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind = 0)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }

        public virtual string GetSZArrayType(string elementType)
        {
            return elementType + "[]";
        }

        public virtual string GetPointerType(string elementType)
        {
            return elementType + "*";
        }

        public virtual string GetByReferenceType(string elementType)
        {
            // Not Distinguishing ByRef in these signatures (like Roslyn)
            //return "ref " + elementType;
            return elementType;
        }

        public virtual string GetGenericMethodParameter(DisassemblingGenericContext genericContext, int index)
        {
            return "!!" + genericContext.MethodParameters[index];
        }

        public virtual string GetGenericTypeParameter(DisassemblingGenericContext genericContext, int index)
        {
            return "!" + genericContext.TypeParameters[index];
        }

        public virtual string GetPinnedType(string elementType)
        {
            return elementType + " pinned";
        }

        public virtual string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            return genericType + "<" + String.Join(", ", typeArguments) + ">";
        }

        private static string RemoveGenericNameSuffix(string name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            int indexOfSuffix = name.IndexOf('`');
            if (indexOfSuffix < 0) return name;

            return name.Substring(0, indexOfSuffix);
        }

        public virtual string GetArrayType(string elementType, ArrayShape shape)
        {
            var builder = new StringBuilder();

            builder.Append(elementType);
            builder.Append('[');

            for (int i = 0; i < shape.Rank; i++)
            {
                int lowerBound = 0;

                if (i < shape.LowerBounds.Length)
                {
                    lowerBound = shape.LowerBounds[i];
                    builder.Append(lowerBound);
                }

                builder.Append("...");

                if (i < shape.Sizes.Length)
                {
                    builder.Append(lowerBound + shape.Sizes[i] - 1);
                }

                if (i < shape.Rank - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(']');
            return builder.ToString();
        }

        public virtual string GetTypeFromHandle(MetadataReader reader, DisassemblingGenericContext genericContext, EntityHandle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle);

                case HandleKind.TypeReference:
                    return GetTypeFromReference(reader, (TypeReferenceHandle)handle);

                case HandleKind.TypeSpecification:
                    return GetTypeFromSpecification(reader, genericContext, (TypeSpecificationHandle)handle);

                default:
                    throw new ArgumentOutOfRangeException("handle");
            }
        }

        public virtual string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired)
        {
            return unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifierType + ")";
        }

        public virtual string GetFunctionPointerType(MethodSignature<string> signature)
        {
            ImmutableArray<string> parameterTypes = signature.ParameterTypes;

            int requiredParameterCount = signature.RequiredParameterCount;

            var builder = new StringBuilder();
            builder.Append("method ");
            builder.Append(signature.ReturnType);
            builder.Append(" *(");

            int i;
            for (i = 0; i < requiredParameterCount; i++)
            {
                builder.Append(parameterTypes[i]);
                if (i < parameterTypes.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            if (i < parameterTypes.Length)
            {
                builder.Append("..., ");
                for (; i < parameterTypes.Length; i++)
                {
                    builder.Append(parameterTypes[i]);
                    if (i < parameterTypes.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            builder.Append(')');
            return builder.ToString();
        }
    }

    public static class MissingExtensions
    {
        // Non-Public, originally from:
        // corefx\src\System.Reflection.Metadata\src\System\Reflection\System.Reflection.cs
        // https://github.com/dotnet/corefx/issues/5377
        private const TypeAttributes NestedMask = (TypeAttributes)0x00000006;

        public static bool IsNested(this TypeAttributes flags)
        {
            return (flags & NestedMask) != 0;
        }
    }
}
