using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public class MockClrRuntime : IClrRuntime
{
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

    internal Func<ulong, string?> GetJitHelperFunctionNameFunc =
        _ => throw new InvalidOperationException("GetJitHelperFunctionNameFunc is not set.");

    internal Func<ulong, IClrMethod?> GetMethodByHandleFunc =
        _ => throw new InvalidOperationException("GetMethodByHandleFunc is not set.");


    internal Func<ulong, IClrMethod?> GetMethodByInstructionPointerFunc =
        _ => throw new InvalidOperationException("GetMethodByInstructionPointerFunc is not set.");


    internal Func<ulong, IClrType?> GetTypeByMethodTableFunc =
        _ => throw new InvalidOperationException("GetTypeByMethodTableFunc is not set.");

    public string? GetJitHelperFunctionName(ulong address)
        => GetJitHelperFunctionNameFunc(address);

    public IClrMethod? GetMethodByHandle(ulong methodHandle)
        => GetMethodByHandleFunc(methodHandle);

    public IClrMethod? GetMethodByInstructionPointer(ulong ip)
        => GetMethodByInstructionPointerFunc(ip);

    public IClrType? GetTypeByMethodTable(ulong methodTable)
        => GetTypeByMethodTableFunc(methodTable);
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
