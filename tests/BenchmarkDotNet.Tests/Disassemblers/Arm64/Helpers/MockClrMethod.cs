using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

internal class MockClrMethod : IClrMethod
{
    private const string UnreachableExceptionMessage = "This class instance methods/properties are not expected to be used.";

    public MockClrMethod(string? name, ulong nativeCode, string? signature, MockClrType? type)
    {
        Name = name;
        NativeCode = nativeCode;
        Signature = signature;
        Type = type;
    }

    public virtual string MethodName =>
        Signature!.Contains(".")
            ? Signature
            : $"{Type.Name}.{Signature}";

    public virtual string? Name
        => field ?? throw new UnreachableException(UnreachableExceptionMessage);

    public virtual ulong NativeCode
    {
        get
        {
            if (field != 0)
                return field;
            throw new UnreachableException(UnreachableExceptionMessage);
        }
    }

    public virtual string? Signature
        => field ?? throw new UnreachableException(UnreachableExceptionMessage);

    public IClrType Type
        => field ?? throw new UnreachableException(UnreachableExceptionMessage);

    #region Not implemented
    public MethodAttributes Attributes => throw new NotImplementedException();

    public MethodCompilationType CompilationType => throw new NotImplementedException();

    public HotColdRegions HotColdInfo => throw new NotImplementedException();

    public ImmutableArray<ILToNativeMap> ILOffsetMap => throw new NotImplementedException();

    public bool IsClassConstructor => throw new NotImplementedException();

    public bool IsConstructor => throw new NotImplementedException();

    public int MetadataToken => throw new NotImplementedException();

    public ulong MethodDesc => throw new NotImplementedException();

    public ILInfo? GetILInfo()
        => throw new NotImplementedException();

    public int GetILOffset(ulong addr)
        => throw new NotImplementedException();
    #endregion
}
