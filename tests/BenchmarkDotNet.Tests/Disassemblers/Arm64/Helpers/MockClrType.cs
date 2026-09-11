using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Collections.Immutable;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

internal class MockClrType : IClrType
{
    public MockClrType(string? name)
    {
        Name = name;
    }

    public string? Name { get; }


    #region NotImplemented
    public ulong AssemblyLoadContextAddress
        => throw new NotImplementedException();

    public IClrType? BaseType
        => throw new NotImplementedException();

    public int ComponentSize
        => throw new NotImplementedException();

    public IClrType? ComponentType
        => throw new NotImplementedException();

    public bool ContainsPointers
        => throw new NotImplementedException();

    public ClrElementType ElementType
        => throw new NotImplementedException();

    public ImmutableArray<IClrInstanceField> Fields
        => throw new NotImplementedException();

    public GCDesc GCDesc
        => throw new NotImplementedException();

    public IClrHeap Heap
        => throw new NotImplementedException();

    public bool IsArray
        => throw new NotImplementedException();

    public bool IsCollectible
        => throw new NotImplementedException();

    public bool IsEnum
        => throw new NotImplementedException();

    public bool IsException
        => throw new NotImplementedException();

    public bool IsFinalizable
        => throw new NotImplementedException();

    public bool IsFree
        => throw new NotImplementedException();

    public bool IsObjectReference
        => throw new NotImplementedException();

    public bool IsPointer
        => throw new NotImplementedException();

    public bool IsPrimitive
        => throw new NotImplementedException();

    public bool IsShared
        => throw new NotImplementedException();

    public bool IsString
        => throw new NotImplementedException();

    public bool IsValueType
        => throw new NotImplementedException();

    public ulong LoaderAllocatorHandle
        => throw new NotImplementedException();

    public int MetadataToken
        => throw new NotImplementedException();

    public ImmutableArray<IClrMethod> Methods
        => throw new NotImplementedException();

    public ulong MethodTable
        => throw new NotImplementedException();

    public IClrModule Module
        => throw new NotImplementedException();

    public ImmutableArray<IClrStaticField> StaticFields
        => throw new NotImplementedException();

    public int StaticSize => throw new NotImplementedException();

    public TypeAttributes TypeAttributes => throw new NotImplementedException();

    public IClrEnum AsEnum()
        => throw new NotImplementedException();

    public IEnumerable<ClrGenericParameter> EnumerateGenericParameters()
        => throw new NotImplementedException();

    public IEnumerable<ClrInterface> EnumerateInterfaces()
        => throw new NotImplementedException();

    public bool Equals(IClrType? other)
        => throw new NotImplementedException();

    public ulong GetArrayElementAddress(ulong objRef, int index)
        => throw new NotImplementedException();

    public IClrInstanceField? GetFieldByName(string name)
        => throw new NotImplementedException();

    public IClrStaticField? GetStaticFieldByName(string name)
        => throw new NotImplementedException();

    public bool IsFinalizeSuppressed(ulong obj)
        => throw new NotImplementedException();

    public T[]? ReadArrayElements<T>(ulong objRef, int start, int count) where T : unmanaged
        => throw new NotImplementedException();
    #endregion
}
