using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Buffers.Binary;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

internal sealed class MockMemory
{
    private readonly List<MemoryRegion> _regions = new();

    private readonly Dictionary<ulong, string?> JitHelperFunctionNames = new();
    private Dictionary<ulong, IClrMethod?> MethodByHandle = new();
    private Dictionary<ulong, IClrMethod?> MethodByInstructionPointer = new();
    private Dictionary<ulong, IClrType?> TypeByMethodTable = new();

    private readonly ulong BaseAddress;

    public MockMemory(ulong baseAddress = 0)
    {
        BaseAddress = baseAddress;
    }

    public MockMemory AddBytes(ulong address, byte[] bytes)
    {
        if (bytes.Length == 0)
            throw new ArgumentException("Memory region cannot be empty.", nameof(bytes));

        address = checked(BaseAddress + address);

        foreach (var region in _regions)
        {
            if (IsOverlapping(address, bytes.Length, region.Address, region.Bytes.Length))
            {
                throw new ArgumentException(
                    $"Memory region 0x{address:X}-0x{GetEndAddress(address, bytes.Length):X} " +
                    $"overlaps existing region " +
                    $"0x{region.Address:X}-0x{GetEndAddress(region.Address, region.Bytes.Length):X}.");
            }
        }

        _regions.Add(new MemoryRegion(address, bytes));

        return this;
    }

    public MockMemory AddInstructions(ulong address, uint[] instructions)
    {
        var bytes = new byte[instructions.Length * sizeof(uint)];

        for (int i = 0; i < instructions.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint)), instructions[i]);
        }

        return AddBytes(address, bytes);
    }

    public MockMemory AddPointer(ulong address, ulong value)
    {
        var bytes = new byte[sizeof(ulong)];

        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);

        return AddBytes(address, bytes);
    }

    public MockMemory AddJitHelperFunctionName(ulong address, string? value)
    {
        JitHelperFunctionNames.Add(address, value);
        return this;
    }

    public MockMemory AddMethodByHandle(ulong methodHandle, IClrMethod? value)
    {
        MethodByHandle.Add(methodHandle, value);
        return this;
    }

    public MockMemory AddMethodByInstructionPointer(ulong ip, IClrMethod? value)
    {
        MethodByInstructionPointer.Add(ip, value);
        return this;
    }

    public MockMemory AddTypeByMethodTable(ulong methodTable, IClrType? value)
    {
        TypeByMethodTable.Add(methodTable, value);
        return this;
    }

    public MockClrRuntime ToMockClrRuntime()
    {
        var dataReader = new MockDataReader(Read, TryReadPointer);
        var clrRuntime = new MockClrRuntime(new MockDataTarget(dataReader));

        // Add additional mappings
        foreach (var item in JitHelperFunctionNames)
            clrRuntime.JitHelperFunctionNames.Add(item.Key, item.Value);

        foreach (var item in MethodByHandle)
            clrRuntime.MethodByHandle.Add(item.Key, item.Value);

        foreach (var item in MethodByInstructionPointer)
            clrRuntime.MethodByInstructionPointer.Add(item.Key, item.Value);

        foreach (var item in TypeByMethodTable)
            clrRuntime.TypeByMethodTable.Add(item.Key, item.Value);

        return clrRuntime;
    }


    private int Read(ulong address, Span<byte> buffer)
    {
        if (buffer.Length == 0)
            return 0;

        var region = FindRegion(address);
        if (region is null)
            throw new ArgumentException($"Failed to read address 0x{address:X}. Because address data is not registered.");

        ulong offset = address - region.Address;
        int available = region.Bytes.Length - (int)offset;
        int count = Math.Min(buffer.Length, available);

        region.Bytes.AsSpan((int)offset, count).CopyTo(buffer);

        return count;
    }

    private bool TryReadPointer(ulong address, out ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];

        if (Read(address, buffer) != sizeof(ulong))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return true;
    }

    private MemoryRegion? FindRegion(ulong address)
    {
        foreach (var region in _regions)
        {
            if (address < region.Address)
                continue;

            ulong offset = address - region.Address;

            if (offset < (ulong)region.Bytes.Length)
                return region;
        }

        return null;
    }

    private static bool IsOverlapping(ulong address1, int length1, ulong address2, int length2)
    {
        ulong end1 = checked(address1 + (ulong)length1);
        ulong end2 = checked(address2 + (ulong)length2);

        return address1 < end2 && address2 < end1;
    }

    private static ulong GetEndAddress(ulong address, int length)
        => checked(address + (ulong)length);

    private sealed class MemoryRegion(ulong address, byte[] bytes)
    {
        public ulong Address { get; } = address;

        public byte[] Bytes { get; } = bytes;
    }
}