using BenchmarkDotNet.Detectors.Smbios;
using BenchmarkDotNet.Extensions;
using System.Buffers.Binary;
using System.Text;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

internal sealed class SmbiosTableBuilder
{
    private readonly byte length;

    private readonly List<byte> buffer = new();

    private readonly List<string> strings = new();

    internal SmbiosTableBuilder(SmbiosType type, byte length)
    {
        this.length = length;

        buffer.Add((byte) type); // 0x00: type
        buffer.Add(length);      // 0x01: length
    }

    internal void Add(string? value)
    {
        if (value.IsBlank())
        {
            buffer.Add(0); // Use 0 as string index
            return;
        }

        int index = strings.IndexOf(value);
        if (index < 0)
        {
            strings.Add(value);
            index = strings.Count - 1;
        }

        if (index + 1 > byte.MaxValue)
            throw new OverflowException();

        buffer.Add((byte) (index + 1)); // Use 1-based index
    }

    internal void Add(byte value)
        => buffer.Add(value);

    internal void Add(byte[] bytes)
        => buffer.AddRange(bytes);

    internal void Add(ushort value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        WriteBytes(bytes);
    }

    internal void Add(uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        WriteBytes(bytes);
    }

    internal void Add(ulong value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        WriteBytes(bytes);
    }

    internal byte[] GetBytes()
    {
        // Trim extra bytes. (It's used for testing compatibility between SMBIOS versions)
        if (buffer.Count >= length)
            buffer.RemoveRange(length, buffer.Count - length);

        // Append string-set bytes if exists.
        if (strings.Count > 0)
        {
            foreach (string s in strings)
            {
                Add(Encoding.UTF8.GetBytes(s));
                Add(0);
            }
            Add(0);
        }
        else
        {
            Add([0, 0]); // Add extra null when no string-sets exists.
        }

        return buffer.ToArray();
    }

    private void WriteBytes(ReadOnlySpan<byte> values)
    {
#if NET
        buffer.AddRange(values);
#else
        buffer.AddRange(values.ToArray());
#endif
    }
}
