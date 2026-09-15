using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public class MockClrRuntime : IClrRuntime
{
    // It is expected that this property will be used for validating registered addresses.
    private bool ThrowExceptionMode => false;

    internal Dictionary<ulong, string?> JitHelperFunctionNames = new();
    internal Dictionary<ulong, IClrMethod?> MethodByHandle = new();
    internal Dictionary<ulong, IClrMethod?> MethodByInstructionPointer = new();
    internal Dictionary<ulong, IClrType?> TypeByMethodTable = new();

    public MockClrRuntime(IDataTarget dataTarget)
    {
        DataTarget = dataTarget;
    }

    public IDataTarget DataTarget { get; }

    public void FlushCachedData() { }

    public void Dispose()
    {
        DataTarget.Dispose();
    }

    #region APIs that are used by TryTranslateAddressToName
    public string? GetJitHelperFunctionName(ulong address)
    {
        if (JitHelperFunctionNames.TryGetValue(address, out var value))
            return value;
        if (ThrowExceptionMode)
            throw new ArgumentException($"Specified address(0x{address:X}) is not registered.");
        return null;
    }

    public IClrMethod? GetMethodByHandle(ulong methodHandle)
    {
        if (MethodByHandle.TryGetValue(methodHandle, out var value))
            return value;
        if (ThrowExceptionMode)
            throw new ArgumentException($"Specified methodHandle(0x{methodHandle:X}) is not registered.");
        return null;
    }

    public IClrMethod? GetMethodByInstructionPointer(ulong ip)
    {
        if (MethodByInstructionPointer.TryGetValue(ip, out var value))
            return value;
        if (ThrowExceptionMode)
            throw new ArgumentException($"Specified InstructionPointer(0x{ip:X}) is not registered.");
        return null;
    }

    public IClrType? GetTypeByMethodTable(ulong methodTable)
    {
        if (TypeByMethodTable.TryGetValue(methodTable, out var value))
            return value;
        if (ThrowExceptionMode)
            throw new ArgumentException($"Specified methodTable(0x{methodTable:X}) is not registered.");
        return null;
    }
    #endregion

    #region Methods/Properties that is not used
    public ImmutableArray<IClrAppDomain> AppDomains
        => throw new NotImplementedException();

    public IClrModule BaseClassLibrary
        => throw new NotImplementedException();

    public IClrInfo ClrInfo
        => throw new NotImplementedException();

    public IClrHeap Heap
        => throw new NotImplementedException();

    public bool IsThreadSafe
        => throw new NotImplementedException();

    public IClrAppDomain? SharedDomain
        => throw new NotImplementedException();

    public IClrAppDomain? SystemDomain
        => throw new NotImplementedException();

    public ImmutableArray<IClrThread> Threads
        => throw new NotImplementedException();

    public IClrThreadPool? ThreadPool
        => throw new NotImplementedException();

    public uint? TlsSlotIndex
        => throw new NotImplementedException();

    public IEnumerable<ClrNativeHeapInfo> EnumerateClrNativeHeaps()
        => throw new NotImplementedException();

    public IEnumerable<IClrRoot> EnumerateHandles()
        => throw new NotImplementedException();

    public IEnumerable<IClrJitManager> EnumerateJitManagers()
        => throw new NotImplementedException();

    public IEnumerable<IClrModule> EnumerateModules()
        => throw new NotImplementedException();

    public IEnumerable<ClrRcwCleanupData> EnumerateRcwCleanupData()
        => throw new NotImplementedException();

    public IEnumerable<ClrSyncBlockCleanupData> EnumerateSyncBlockCleanupData()
        => throw new NotImplementedException();
    #endregion
}
