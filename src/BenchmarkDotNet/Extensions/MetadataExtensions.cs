using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace BenchmarkDotNet.Extensions
{
    /// <summary>
    /// Get benchmarks by reading metadata, this allows us to inspect any .NET DLL agnostic of runtime.
    /// For example, we can open a .NET Core benchmark DLL with .NET Framework.
    /// Alternatively, you can use <see cref="ReflectionExtensions"/>.
    /// </summary>
    public static class MetadataExtensions
    {
        /// <summary>
        /// Get a list of benchmarks grouped by class name
        /// </summary>
        /// <param name="assembly">assembly we are interested in</param>
        /// <returns>a dictionary where the keys are classes and the values are the benchmarks contained in the classes</returns>
        public static Dictionary<string, List<string>> GetRunnableBenchmarksNames(this Assembly assembly)
        {
            var ret = new Dictionary<string, List<string>>();
            using var fs = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);

            MetadataReader mr = peReader.GetMetadataReader();
            var typeDefs = mr.TypeDefinitions.Select(t => t.GetTypeDefinition(mr)).ToArray();
            foreach (var t in typeDefs)
            {
                var benchmarks = t.RunnableBenchmarks(mr).ToList();
                var name = t.Name.GetString(mr);
                ret.Add(name, benchmarks);
            }

            return ret;
        }

        internal static IEnumerable<string> RunnableBenchmarks(this TypeDefinition type, MetadataReader mr)
        {
            bool isAbstract = (type.Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
            bool isSealed = (type.Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;

            bool isNotPublic = (type.Attributes & TypeAttributes.Public) != TypeAttributes.Public;

            if (isAbstract || isSealed || isNotPublic)
            {
                return Enumerable.Empty<string>();
            }

            return type.GetBenchmarks(mr);
        }

        private static IEnumerable<string> GetBenchmarks(this TypeDefinition typeDef, MetadataReader mr)
        {
            var methods = typeDef.GetMethods().Select(t => t.GetMethodDefinition(mr));
            foreach (var method in methods)
            {
                var isPublic = (method.Attributes & MethodAttributes.Public) == MethodAttributes.Public;
                var isInstance = (method.Attributes & MethodAttributes.Static) != MethodAttributes.Static;
                if (!isPublic || !isInstance)
                {
                    continue;
                }

                var customAttrs = method.GetCustomAttributes().Select(a => a.GetCustomAttribute(mr));
                var isBenchmark = customAttrs.Any(a =>
                {
                    MemberReference memberRef = ((MemberReferenceHandle)a.Constructor).GetMemberReference(mr);
                    var name = ((TypeReferenceHandle)memberRef.Parent).GetTypeReference(mr).Name.GetString(mr);
                    return name == nameof(BenchmarkAttribute);
                });

                if (!isBenchmark)
                {
                    continue;
                }

                yield return method.Name.GetString(mr);
            }
        }

        private static AssemblyFile GetAssemblyFile(this AssemblyFileHandle handle, MetadataReader reader) => reader.GetAssemblyFile(handle);
        private static AssemblyReference GetAssemblyReference(this AssemblyReferenceHandle handle, MetadataReader reader) => reader.GetAssemblyReference(handle);
        private static byte[] GetBlobBytes(this BlobHandle handle, MetadataReader reader) => reader.GetBlobBytes(handle);
        private static ImmutableArray<byte> GetBlobContent(this BlobHandle handle, MetadataReader reader) => reader.GetBlobContent(handle);
        private static BlobReader GetBlobReader(this BlobHandle handle, MetadataReader reader) => reader.GetBlobReader(handle);
        private static BlobReader GetBlobReader(this StringHandle handle, MetadataReader reader) => reader.GetBlobReader(handle);
        private static Constant GetConstant(this ConstantHandle handle, MetadataReader reader) => reader.GetConstant(handle);
        private static CustomAttribute GetCustomAttribute(this CustomAttributeHandle handle, MetadataReader reader) => reader.GetCustomAttribute(handle);
        private static CustomAttributeHandleCollection GetCustomAttributes(this EntityHandle handle, MetadataReader reader) => reader.GetCustomAttributes(handle);
        private static CustomDebugInformation GetCustomDebugInformation(this CustomDebugInformationHandle handle, MetadataReader reader) => reader.GetCustomDebugInformation(handle);
        private static CustomDebugInformationHandleCollection GetCustomDebugInformation(this EntityHandle handle, MetadataReader reader) => reader.GetCustomDebugInformation(handle);
        private static DeclarativeSecurityAttribute GetDeclarativeSecurityAttribute(this DeclarativeSecurityAttributeHandle handle, MetadataReader reader) => reader.GetDeclarativeSecurityAttribute(handle);
        private static Document GetDocument(this DocumentHandle handle, MetadataReader reader) => reader.GetDocument(handle);
        private static EventDefinition GetEventDefinition(this EventDefinitionHandle handle, MetadataReader reader) => reader.GetEventDefinition(handle);
        private static ExportedType GetExportedType(this ExportedTypeHandle handle, MetadataReader reader) => reader.GetExportedType(handle);
        private static FieldDefinition GetFieldDefinition(this FieldDefinitionHandle handle, MetadataReader reader) => reader.GetFieldDefinition(handle);
        private static GenericParameter GetGenericParameter(this GenericParameterHandle handle, MetadataReader reader) => reader.GetGenericParameter(handle);
        private static GenericParameterConstraint GetGenericParameterConstraint(this GenericParameterConstraintHandle handle, MetadataReader reader) => reader.GetGenericParameterConstraint(handle);
        private static Guid GetGuid(this GuidHandle handle, MetadataReader reader) => reader.GetGuid(handle);
        private static ImportScope GetImportScope(this ImportScopeHandle handle, MetadataReader reader) => reader.GetImportScope(handle);
        private static InterfaceImplementation GetInterfaceImplementation(this InterfaceImplementationHandle handle, MetadataReader reader) => reader.GetInterfaceImplementation(handle);
        private static LocalConstant GetLocalConstant(this LocalConstantHandle handle, MetadataReader reader) => reader.GetLocalConstant(handle);
        private static LocalScope GetLocalScope(this LocalScopeHandle handle, MetadataReader reader) => reader.GetLocalScope(handle);
        private static LocalScopeHandleCollection GetLocalScopes(this MethodDefinitionHandle handle, MetadataReader reader) => reader.GetLocalScopes(handle);
        private static LocalScopeHandleCollection GetLocalScopes(this MethodDebugInformationHandle handle, MetadataReader reader) => reader.GetLocalScopes(handle);
        private static LocalVariable GetLocalVariable(this LocalVariableHandle handle, MetadataReader reader) => reader.GetLocalVariable(handle);
        private static ManifestResource GetManifestResource(this ManifestResourceHandle handle, MetadataReader reader) => reader.GetManifestResource(handle);
        private static MemberReference GetMemberReference(this MemberReferenceHandle handle, MetadataReader reader) => reader.GetMemberReference(handle);
        private static MethodDebugInformation GetMethodDebugInformation(this MethodDebugInformationHandle handle, MetadataReader reader) => reader.GetMethodDebugInformation(handle);
        private static MethodDebugInformation GetMethodDebugInformation(this MethodDefinitionHandle handle, MetadataReader reader) => reader.GetMethodDebugInformation(handle);
        private static MethodDefinition GetMethodDefinition(this MethodDefinitionHandle handle, MetadataReader reader) => reader.GetMethodDefinition(handle);
        private static MethodImplementation GetMethodImplementation(this MethodImplementationHandle handle, MetadataReader reader) => reader.GetMethodImplementation(handle);
        private static MethodSpecification GetMethodSpecification(this MethodSpecificationHandle handle, MetadataReader reader) => reader.GetMethodSpecification(handle);
        private static ModuleReference GetModuleReference(this ModuleReferenceHandle handle, MetadataReader reader) => reader.GetModuleReference(handle);
        private static NamespaceDefinition GetNamespaceDefinition(this NamespaceDefinitionHandle handle, MetadataReader reader) => reader.GetNamespaceDefinition(handle);
        private static Parameter GetParameter(this ParameterHandle handle, MetadataReader reader) => reader.GetParameter(handle);
        private static PropertyDefinition GetPropertyDefinition(this PropertyDefinitionHandle handle, MetadataReader reader) => reader.GetPropertyDefinition(handle);
        private static StandaloneSignature GetStandaloneSignature(this StandaloneSignatureHandle handle, MetadataReader reader) => reader.GetStandaloneSignature(handle);
        private static string GetString(this StringHandle handle, MetadataReader reader) => reader.GetString(handle);
        private static string GetString(this NamespaceDefinitionHandle handle, MetadataReader reader) => reader.GetString(handle);
        private static string GetString(this DocumentNameBlobHandle handle, MetadataReader reader) => reader.GetString(handle);
        private static TypeDefinition GetTypeDefinition(this TypeDefinitionHandle handle, MetadataReader reader) => reader.GetTypeDefinition(handle);
        private static TypeReference GetTypeReference(this TypeReferenceHandle handle, MetadataReader reader) => reader.GetTypeReference(handle);
        private static TypeSpecification GetTypeSpecification(this TypeSpecificationHandle handle, MetadataReader reader) => reader.GetTypeSpecification(handle);
        private static string GetUserString(this UserStringHandle handle, MetadataReader reader) => reader.GetUserString(handle);

        private static int GetToken(this Handle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this EntityHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this TypeDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this TypeReferenceHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this TypeSpecificationHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this GenericParameterHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this GenericParameterConstraintHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this FieldDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this EventDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this MethodDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this PropertyDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this ParameterHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this StandaloneSignatureHandle handle) => MetadataTokens.GetToken(handle);
        private static int GetToken(this AssemblyFileHandle handle) => MetadataTokens.GetToken(handle);

        private static string GetStringOrNull(this StringHandle handle, MetadataReader reader) => handle.IsNil ? null : reader.GetString(handle);

        private static bool Equals(this StringHandle handle, string value, MetadataReader reader) => reader.StringComparer.Equals(handle, value, ignoreCase: false);

        //
        // utf8.Length does *not* include NUL terminator.
        //
        private static unsafe bool Equals(this StringHandle handle, ReadOnlySpan<byte> utf8, MetadataReader reader)
        {
            //TODO: Perf - GetBlobReader() scans the string handle for a NUL terminator to compute the length making it an O(N)
            // operation. It might be worth memoizing the pointer/length combo per TypeDefToken and ExportedTypeToken. But even better
            // would be to get UTF8 Equals overloads added to MetadataStringComparer.
            BlobReader br = handle.GetBlobReader(reader);
            ReadOnlySpan<byte> actual = new ReadOnlySpan<byte>(br.CurrentPointer, br.Length);
            return utf8.SequenceEqual(actual);
        }

        private static Handle ToHandle(this int token) => MetadataTokens.Handle(token);
        private static TypeDefinitionHandle ToTypeDefinitionHandle(this int token) => MetadataTokens.TypeDefinitionHandle(token);
        private static TypeReferenceHandle ToTypeReferenceHandle(this int token) => MetadataTokens.TypeReferenceHandle(token);
        private static TypeSpecificationHandle ToTypeSpecificationHandle(this int token) => MetadataTokens.TypeSpecificationHandle(token);
    }
}
