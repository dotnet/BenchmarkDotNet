using System.Text;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Processor information from an SMBIOS Type 4 (Processor Information) structure.
/// Every property maps one-to-one to a field of that structure;
/// the values that must be derived from several fields are exposed as separate
/// <c>Resolved*</c> properties. A speed of 0 means Unknown, a count of 0 means Unknown,
/// and an all-zero Processor ID means Unknown.
/// </summary>
internal sealed class SmbiosProcessorInfo : ISmbiosStructureInfo
{
    public SmbiosType Type => SmbiosType.ProcessorInformation;

    /// <summary>
    /// Parses a Type 4 formatted area (<paramref name="bytes"/>) and its
    /// string-set (<paramref name="strings"/>), setting every raw property.
    /// </summary>
    internal SmbiosProcessorInfo(ReadOnlySpan<byte> bytes, IReadOnlyList<string> strings)
    {
        // Type 4 offsets are relative to the structure start.
#pragma warning disable format
        Handle                   = ProcessorOffsets.Handle.Read(bytes);                         // 0x02
        SocketDesignation        = ProcessorOffsets.SocketDesignation.Read(bytes, strings);     // 0x04
        ProcessorType            = ProcessorOffsets.ProcessorType.Read(bytes);                  // 0x05
        ProcessorFamily          = ProcessorOffsets.ProcessorFamily.Read(bytes);                // 0x06
        ProcessorManufacturer    = ProcessorOffsets.ProcessorManufacturer.Read(bytes, strings); // 0x07
        ProcessorId              = TryReadProcessorId(bytes);                                   // 0x08
        ProcessorVersion         = ProcessorOffsets.ProcessorVersion.Read(bytes, strings);      // 0x10
        Voltage                  = ProcessorOffsets.Voltage.Read(bytes);                        // 0x11
        ExternalClockMHz         = ProcessorOffsets.ExternalClock.Read(bytes);                  // 0x12
        MaxSpeedMHz              = ProcessorOffsets.MaxSpeed.Read(bytes);                       // 0x14
        CurrentSpeedMHz          = ProcessorOffsets.CurrentSpeed.Read(bytes);                   // 0x16
        Status                   = ProcessorOffsets.Status.Read(bytes);                         // 0x18
        ProcessorUpgrade         = ProcessorOffsets.ProcessorUpgrade.Read(bytes);               // 0x19
        L1CacheHandle            = ProcessorOffsets.L1CacheHandle.Read(bytes);                  // 0x1A
        L2CacheHandle            = ProcessorOffsets.L2CacheHandle.Read(bytes);                  // 0x1C
        L3CacheHandle            = ProcessorOffsets.L3CacheHandle.Read(bytes);                  // 0x1E
        SerialNumber             = ProcessorOffsets.SerialNumber.Read(bytes, strings);          // 0x20
        AssetTag                 = ProcessorOffsets.AssetTag.Read(bytes, strings);              // 0x21
        PartNumber               = ProcessorOffsets.PartNumber.Read(bytes, strings);            // 0x22
        CoreCount                = ProcessorOffsets.CoreCount.Read(bytes);                      // 0x23
        CoreEnabled              = ProcessorOffsets.CoreEnabled.Read(bytes);                    // 0x24
        ThreadCount              = ProcessorOffsets.ThreadCount.Read(bytes);                    // 0x25
        ProcessorCharacteristics = ProcessorOffsets.ProcessorCharacteristics.Read(bytes);       // 0x26
        ProcessorFamily2         = ProcessorOffsets.ProcessorFamily2.Read(bytes);               // 0x28
        CoreCount2               = ProcessorOffsets.CoreCount2.Read(bytes);                     // 0x2A
        CoreEnabled2             = ProcessorOffsets.CoreEnabled2.Read(bytes);                   // 0x2C
        ThreadCount2             = ProcessorOffsets.ThreadCount2.Read(bytes);                   // 0x2E
        ThreadEnabled            = ProcessorOffsets.ThreadEnabled.Read(bytes);                  // 0x30
        SocketType               = ProcessorOffsets.SocketType.Read(bytes, strings);            // 0x32
#pragma warning restore format

        // CoreCount2/CoreEnabled2/ThreadCount2 were added in SMBIOS 3.0.
        HasCoreCount2 = ProcessorOffsets.CoreCount2.HasField(bytes);
        HasCoreEnabled2 = ProcessorOffsets.CoreEnabled2.HasField(bytes);
        HasThreadCount2 = ProcessorOffsets.ThreadCount2.HasField(bytes);
    }

    #region Raw properties

    /// <summary>
    /// Handle of this structure entry (offset 0x02). 0xFFFF means no handle.
    /// </summary>
    public ushort Handle { get; init; }

    /// <summary>
    /// Socket designation (offset 0x04). Null when not provided.
    /// </summary>
    public string? SocketDesignation { get; init; }

    /// <summary>
    /// Processor type identifier (offset 0x05).
    /// </summary>
    public byte ProcessorType { get; init; }

    /// <summary>
    /// Processor family identifier (offset 0x06).
    /// </summary>
    public byte ProcessorFamily { get; init; }

    /// <summary>
    /// Processor manufacturer (offset 0x07). Null when not provided.
    /// </summary>
    public string? ProcessorManufacturer { get; init; }

    /// <summary>
    /// Processor ID field as an uppercase hex string (offset 0x08).
    /// Null when not provided or all zeros (the processor does not report an ID).
    /// </summary>
    public string? ProcessorId { get; init; }

    /// <summary>
    /// Processor version (offset 0x10). Null when not provided.
    /// </summary>
    public string? ProcessorVersion { get; init; }

    /// <summary>
    /// Voltage (offset 0x11). Deprecated, so 0 means Unknown.
    /// </summary>
    public byte Voltage { get; init; }

    /// <summary>
    /// External clock frequency in MHz (offset 0x12). 0 means Unknown.
    /// </summary>
    public ushort ExternalClockMHz { get; init; }

    /// <summary>
    /// Maximum configurable speed in MHz (offset 0x14). 0 means Unknown.
    /// </summary>
    public ushort MaxSpeedMHz { get; init; }

    /// <summary>
    /// Current speed in MHz (offset 0x16). 0 means Unknown.
    /// </summary>
    public ushort CurrentSpeedMHz { get; init; }

    /// <summary>
    /// Status bitfield (offset 0x18). Bits 2:0 hold the CPU status (1h means "CPU Enabled")
    /// and bit 6 indicates whether the socket is populated.
    /// </summary>
    public byte Status { get; init; }

    /// <summary>
    /// Processor upgrade identifier (offset 0x19).
    /// </summary>
    public byte ProcessorUpgrade { get; init; }

    /// <summary>
    /// L1 cache handle (offset 0x1A). 0xFFFF means no cache information.
    /// </summary>
    public ushort L1CacheHandle { get; init; }

    /// <summary>
    /// L2 cache handle (offset 0x1C). 0xFFFF means no cache information.
    /// </summary>
    public ushort L2CacheHandle { get; init; }

    /// <summary>
    /// L3 cache handle (offset 0x1E). 0xFFFF means no cache information.
    /// </summary>
    public ushort L3CacheHandle { get; init; }

    /// <summary>
    /// Serial number (offset 0x20). Null when not provided.
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// Asset tag (offset 0x21). Null when not provided.
    /// </summary>
    public string? AssetTag { get; init; }

    /// <summary>
    /// Part number (offset 0x22). Null when not provided.
    /// </summary>
    public string? PartNumber { get; init; }

    /// <summary>
    /// Raw core count (offset 0x23). 0xFF selects <see cref="CoreCount2"/>; 0 means Unknown.
    /// </summary>
    public byte CoreCount { get; init; }

    /// <summary>
    /// Raw enabled core count (offset 0x24). 0xFF selects <see cref="CoreEnabled2"/>; 0 means Unknown.
    /// </summary>
    public byte CoreEnabled { get; init; }

    /// <summary>
    /// Raw thread count (offset 0x25). 0xFF selects <see cref="ThreadCount2"/>; 0 means Unknown.
    /// </summary>
    public byte ThreadCount { get; init; }

    /// <summary>
    /// Processor characteristics bitfield (offset 0x26).
    /// </summary>
    public ushort ProcessorCharacteristics { get; init; }

    /// <summary>
    /// Processor family 2 identifier (offset 0x28).
    /// </summary>
    public ushort ProcessorFamily2 { get; init; }

    /// <summary>
    /// Extended core count (offset 0x2A, SMBIOS 3.0+). 0 means Unknown.
    /// </summary>
    public ushort CoreCount2 { get; init; }

    /// <summary>
    /// Extended enabled core count (offset 0x2C, SMBIOS 3.0+). 0 means Unknown.
    /// </summary>
    public ushort CoreEnabled2 { get; init; }

    /// <summary>
    /// Extended thread count (offset 0x2E, SMBIOS 3.0+). 0 means Unknown.
    /// </summary>
    public ushort ThreadCount2 { get; init; }

    /// <summary>
    /// Firmware-enabled thread count (offset 0x30, SMBIOS 3.6+). 0xFFFF is reserved; 0 means Unknown.
    /// </summary>
    public ushort ThreadEnabled { get; init; }

    /// <summary>
    /// Socket type (offset 0x32, SMBIOS 3.8+). Null when not provided.
    /// </summary>
    public string? SocketType { get; init; }
    #endregion

    #region Resolved properties: the final value derived from several raw fields.
    /// <summary>
    /// Number of cores per processor, resolved from <see cref="CoreCount"/> and
    /// <see cref="CoreCount2"/>. 0 means Unknown.
    /// </summary>
    public int ResolvedCoreCount
        => ResolveCount(CoreCount, CoreCount2, HasCoreCount2);

    /// <summary>
    /// Number of enabled cores per processor, resolved from <see cref="CoreEnabled"/> and
    /// <see cref="CoreEnabled2"/>. 0 means Unknown.
    /// </summary>
    public int ResolvedCoreEnabledCount
        => ResolveCount(CoreEnabled, CoreEnabled2, HasCoreEnabled2);

    /// <summary>
    /// Number of threads per processor, resolved from <see cref="ThreadCount"/> and
    /// <see cref="ThreadCount2"/>. 0 means Unknown.
    /// </summary>
    public int ResolvedThreadCount
        => ResolveCount(ThreadCount, ThreadCount2, HasThreadCount2);

    /// <summary>
    /// Number of firmware-enabled threads per processor, resolved from <see cref="ThreadEnabled"/>
    /// (0xFFFF is reserved for Unknown). 0 means Unknown.
    /// </summary>
    public int ThreadEnabledCount
        => ThreadEnabled == 0xFFFF ? 0 : ThreadEnabled;
    #endregion

    /// <summary>
    /// Whether Core Count 2 (offset 0x2A) is present. It was added in SMBIOS 3.0.
    /// </summary>
    public bool HasCoreCount2 { get; init; }

    /// <summary>
    /// Whether Core Enabled 2 (offset 0x2C) is present. It was added in SMBIOS 3.0.
    /// </summary>
    public bool HasCoreEnabled2 { get; init; }

    /// <summary>
    /// Whether Thread Count 2 (offset 0x2E) is present. It was added in SMBIOS 3.0.
    /// </summary>
    public bool HasThreadCount2 { get; init; }

    /// <summary>
    /// Whether the CPU is enabled (Status bits 2:0 == 1h, "CPU Enabled"). A socket can be
    /// populated while its CPU is disabled by the user or the BIOS, and such a socket has
    /// no usable CPU information.
    /// </summary>
    public bool IsEnabled => (Status & 0x07) == 0x01;

    /// <summary>
    /// Whether the socket is populated (Status bit 6).
    /// </summary>
    internal static bool IsPopulated(ReadOnlySpan<byte> bytes)
    {
        // Bit 6 of Status indicates whether the socket is populated.
        return (ProcessorOffsets.Status.Read(bytes) & 0x40) != 0;
    }

    private static int ResolveCount(byte shortCount, ushort extendedCount, bool hasExtended)
    {
        if (shortCount != 0xFF)
            return shortCount; // 0 means unknown

        if (!hasExtended)
            return 0xFF; // The extended field is absent (pre-3.0); best effort.

        return extendedCount == 0xFFFF
            ? 0 // 0xFFFF is reserved for Unknown
            : extendedCount;
    }

    private static string? TryReadProcessorId(ReadOnlySpan<byte> bytes)
    {
        if (!ProcessorOffsets.ProcessorId.HasField(bytes))
            return null;

        // Convert to hex string (Processor ID is 8-bytes and architecture(e.g. x64/arm64) specific format)
        int offset = ProcessorOffsets.ProcessorId.Offset;
        int size = ProcessorOffsets.ProcessorId.Size;
        ReadOnlySpan<byte> processorId = bytes.Slice(offset, size);

        // An all-zero ID means the processor does not report one.
        foreach (byte value in processorId)
        {
            if (value != 0)
                return ToHexString(processorId);
        }

        return null;
    }

    private static string ToHexString(ReadOnlySpan<byte> bytes)
    {
#if NET
        return Convert.ToHexString(bytes);
#else
        var builder = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            builder.Append(b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        return builder.ToString();
#endif
    }

    /// <summary>
    /// Returns a human-readable single-line summary of this processor entry.
    /// Properties whose value is Unknown (missing strings or 0) are omitted.
    /// </summary>
    public string ToDisplayString()
    {
        List<string> values = new();

        if (!ProcessorVersion.IsBlank())
            values.Add(ProcessorVersion.Trim()); // 0x10

        if (ExternalClockMHz > 0)
            values.Add($"External {ExternalClockMHz}MHz"); // 0x12

        if (MaxSpeedMHz > 0)
            values.Add($"Max {MaxSpeedMHz}MHz"); // 0x14

        if (CurrentSpeedMHz > 0)
            values.Add($"Current {CurrentSpeedMHz}MHz"); // 0x16

        if (ThreadEnabledCount != 0)
        {
            if (ResolvedCoreCount > 0)
            {
                string segment = $"{ResolvedCoreCount} cores"; // 0x23 + 0x2A
                if (ResolvedCoreEnabledCount > 0)
                    segment += $" ({ResolvedCoreEnabledCount} enabled)"; // 0x24 + 0x2C
                values.Add(segment);
            }
        }
        else if (ResolvedThreadCount > 0)
        {
            string segment = $"{ResolvedThreadCount} threads"; // 0x25 + 0x2E
            if (ThreadEnabledCount > 0)
                segment += $" ({ThreadEnabledCount} enabled)"; // 0x30
            values.Add(segment);
        }

        return string.Join(", ", values);
    }

    public override string ToString()
        => ToDisplayString();

    /// <summary>
    /// Type 4 field offsets (DSP0134 7.5, Processor Information).
    /// Each offset is bundled with its data width.
    /// </summary>
    private static class ProcessorOffsets
    {
#pragma warning disable format
        internal static readonly UInt16Field Handle                   = new(0x02);
        internal static readonly StringField SocketDesignation        = new(0x04);
        internal static readonly ByteField   ProcessorType            = new(0x05);
        internal static readonly ByteField   ProcessorFamily          = new(0x06);
        internal static readonly StringField ProcessorManufacturer    = new(0x07);
        internal static readonly UInt64Field ProcessorId              = new(0x08);
        internal static readonly StringField ProcessorVersion         = new(0x10);
        internal static readonly ByteField   Voltage                  = new(0x11);
        internal static readonly UInt16Field ExternalClock            = new(0x12);
        internal static readonly UInt16Field MaxSpeed                 = new(0x14);
        internal static readonly UInt16Field CurrentSpeed             = new(0x16);
        internal static readonly ByteField   Status                   = new(0x18);
        internal static readonly ByteField   ProcessorUpgrade         = new(0x19);
        internal static readonly UInt16Field L1CacheHandle            = new(0x1A);
        internal static readonly UInt16Field L2CacheHandle            = new(0x1C);
        internal static readonly UInt16Field L3CacheHandle            = new(0x1E);
        internal static readonly StringField SerialNumber             = new(0x20);
        internal static readonly StringField AssetTag                 = new(0x21);
        internal static readonly StringField PartNumber               = new(0x22);
        internal static readonly ByteField   CoreCount                = new(0x23);
        internal static readonly ByteField   CoreEnabled              = new(0x24);
        internal static readonly ByteField   ThreadCount              = new(0x25);
        internal static readonly UInt16Field ProcessorCharacteristics = new(0x26);
        internal static readonly UInt16Field ProcessorFamily2         = new(0x28);
        internal static readonly UInt16Field CoreCount2               = new(0x2A);
        internal static readonly UInt16Field CoreEnabled2             = new(0x2C);
        internal static readonly UInt16Field ThreadCount2             = new(0x2E);
        internal static readonly UInt16Field ThreadEnabled            = new(0x30);
        internal static readonly StringField SocketType               = new(0x32);
#pragma warning restore format
    }
}
