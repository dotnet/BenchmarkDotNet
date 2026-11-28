using System.Buffers.Binary;

namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// SMBIOS field descriptor: an offset bundled with its data width.
/// </summary>
internal interface ISmbiosField
{
    int Offset { get; }

    int Size { get; }
}

/// <summary>
/// SMBIOS value field descriptor. <see cref="Unknown"/> is returned when the field
/// is absent (e.g. older SMBIOS revisions with shorter formatted areas).
/// </summary>
internal interface ISmbiosValueField<T> : ISmbiosField
{
    T Unknown { get; }

    T Read(ReadOnlySpan<byte> bytes);
}

/// <summary>
/// SMBIOS field offset bundled with its data width.
/// </summary>
internal readonly record struct ByteField(int Offset) : ISmbiosValueField<byte>
{
    public int Size => 1;

    public byte Unknown => 0;

    public bool HasField(ReadOnlySpan<byte> bytes)
        => Offset >= 0 && Offset + Size <= bytes.Length;

    public byte Read(ReadOnlySpan<byte> bytes)
        => HasField(bytes) ? bytes[Offset] : Unknown;
}

/// <summary>
/// SMBIOS string-index field offset.
/// Resolution to the string-set happens at read time.
/// </summary>
internal readonly record struct StringField(int Offset) : ISmbiosField
{
    public int Size => 1;

    public bool HasField(ReadOnlySpan<byte> bytes)
        => Offset >= 0 && Offset + Size <= bytes.Length;

    public string? Read(ReadOnlySpan<byte> bytes, IReadOnlyList<string> strings)
    {
        byte index = HasField(bytes)
            ? bytes[Offset]
            : (byte) 0;

        // SMBIOS string indices are 1-based; 0 means "no string".
        if (index == 0 || index > strings.Count)
            return null;

        string value = strings[index - 1];
        return string.IsNullOrEmpty(value)
            ? null
            : value;
    }
}

/// <summary>
/// SMBIOS field offset bundled with its data width.
/// </summary>
internal readonly record struct UInt16Field(int Offset, ushort Unknown = 0) : ISmbiosValueField<ushort>
{
    public int Size => 2;

    public bool HasField(ReadOnlySpan<byte> bytes)
        => Offset >= 0 && Offset + Size <= bytes.Length;

    public ushort Read(ReadOnlySpan<byte> bytes)
        => HasField(bytes)
            ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(Offset, 2))
            : Unknown;
}

/// <summary>
/// SMBIOS field offset bundled with its data width.
/// </summary>
internal readonly record struct UInt32Field(int Offset) : ISmbiosValueField<uint>
{
    public int Size => 4;

    public uint Unknown => 0;

    public bool HasField(ReadOnlySpan<byte> bytes)
        => Offset >= 0 && Offset + Size <= bytes.Length;

    public uint Read(ReadOnlySpan<byte> bytes)
        => HasField(bytes)
            ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(Offset, 4))
            : Unknown;
}

/// <summary>
/// SMBIOS field offset bundled with its data width.
/// </summary>
internal readonly record struct UInt64Field(int Offset) : ISmbiosValueField<ulong>
{
    public int Size => 8;

    public ulong Unknown => 0;

    public bool HasField(ReadOnlySpan<byte> bytes)
        => Offset >= 0 && Offset + Size <= bytes.Length;

    public ulong Read(ReadOnlySpan<byte> bytes)
        => HasField(bytes)
            ? BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(Offset, 8))
            : Unknown;
}
