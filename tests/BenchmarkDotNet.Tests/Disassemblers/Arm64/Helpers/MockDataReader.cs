using Microsoft.Diagnostics.Runtime;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public delegate int ReadBytesDelegate(ulong address, Span<byte> buffer);
public delegate bool TryReadPointerDelegate(ulong address, out ulong value);

internal class MockDataReader : IDataReader
{
    private readonly ReadBytesDelegate _read = (_, _) => throw new InvalidOperationException($"{nameof(_read)} field is not set.");
    private readonly TryReadPointerDelegate _tryReadPointer = (_, out _) => throw new InvalidOperationException($"{nameof(_tryReadPointer)} field is not set.");


    public MockDataReader(ulong dummyValue = 0)
    {
        _tryReadPointer = (ulong address, out ulong value) =>
        {
            value = dummyValue;
            return true;
        };
    }

    public MockDataReader(TryReadPointerDelegate tryReadPointer)
    {
        _tryReadPointer = tryReadPointer;
    }

    public MockDataReader(ReadBytesDelegate read)
    {
        _read = read;
    }

    public MockDataReader(ReadBytesDelegate read, TryReadPointerDelegate tryReadPointer)
    {
        _read = read;
        _tryReadPointer = tryReadPointer;
    }

    public void FlushCachedData()
    {
    }

    public int PointerSize
        => 8;

    public ulong ReadPointer(ulong address)
    {
        if (_tryReadPointer(address, out var value))
            return value;

        return 0;
    }

    // It's used by TryResolvePrecode
    public int Read(ulong address, Span<byte> buffer)
        => _read(address, buffer);

    // It's used by TryResolvePrecode
    public bool ReadPointer(ulong address, out ulong value)
        => _tryReadPointer(address, out value);

    #region Methods/Properties that is not used
    public string DisplayName
        => throw new NotImplementedException();

    public bool IsThreadSafe
        => throw new NotImplementedException();

    public OSPlatform TargetPlatform
        => throw new NotImplementedException();

    public Architecture Architecture
        => throw new NotImplementedException();

    public int ProcessId
        => throw new NotImplementedException();

    public IEnumerable<ModuleInfo> EnumerateModules()
        => throw new NotImplementedException();

    public bool GetThreadContext(uint threadID, uint contextFlags, Span<byte> context)
        => throw new NotImplementedException();

    public bool Read<T>(ulong address, out T value) where T : unmanaged
        => throw new NotImplementedException();

    public T Read<T>(ulong address) where T : unmanaged
        => throw new NotImplementedException();
    #endregion
}
