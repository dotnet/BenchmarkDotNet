using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Memory device information from an SMBIOS Type 17 (Memory Device) structure.
/// Every property maps one-to-one to a field of that structure;
/// the values that must be derived from several fields are exposed as separate
/// <c>Resolved*</c> properties. A value of 0 means Unknown, and Size = 0 means
/// no module is installed in the socket.
/// </summary>
internal sealed class SmbiosMemoryDeviceInfo : ISmbiosStructureInfo
{
    public SmbiosType Type => SmbiosType.MemoryDevice;

    /// <summary>
    /// Parses a Type 17 formatted area (<paramref name="bytes"/>) and its
    /// string-set (<paramref name="strings"/>), setting every raw property.
    /// </summary>
    internal SmbiosMemoryDeviceInfo(ReadOnlySpan<byte> bytes, IReadOnlyList<string> strings)
    {
        // Type 17 offsets are relative to the structure start.
        Handle = MemoryDeviceOffsets.Handle.Read(bytes);                                             // 0x02
        PhysicalMemoryArrayHandle = MemoryDeviceOffsets.PhysicalMemoryArrayHandle.Read(bytes);       // 0x04
        MemoryErrorInformationHandle = MemoryDeviceOffsets.MemoryErrorInformationHandle.Read(bytes); // 0x06
        TotalWidth = MemoryDeviceOffsets.TotalWidth.Read(bytes);                                     // 0x08
        DataWidth = MemoryDeviceOffsets.DataWidth.Read(bytes);                                       // 0x0A
        Size = MemoryDeviceOffsets.Size.Read(bytes);                                                 // 0x0C
        FormFactorId = MemoryDeviceOffsets.FormFactor.Read(bytes);                                   // 0x0E
        DeviceSet = MemoryDeviceOffsets.DeviceSet.Read(bytes);                                       // 0x0F
        DeviceLocator = MemoryDeviceOffsets.DeviceLocator.Read(bytes, strings);                      // 0x10
        BankLocator = MemoryDeviceOffsets.BankLocator.Read(bytes, strings);                          // 0x11
        MemoryTypeId = MemoryDeviceOffsets.MemoryType.Read(bytes);                                   // 0x12
        TypeDetail = MemoryDeviceOffsets.TypeDetail.Read(bytes);                                     // 0x13
        Speed = MemoryDeviceOffsets.Speed.Read(bytes);                                               // 0x15
        Manufacturer = MemoryDeviceOffsets.Manufacturer.Read(bytes, strings);                        // 0x17
        SerialNumber = MemoryDeviceOffsets.SerialNumber.Read(bytes, strings);                        // 0x18
        AssetTag = MemoryDeviceOffsets.AssetTag.Read(bytes, strings);                                // 0x19
        PartNumber = MemoryDeviceOffsets.PartNumber.Read(bytes, strings);                            // 0x1A
        Attributes = MemoryDeviceOffsets.Attributes.Read(bytes);                                     // 0x1B
        ExtendedSize = MemoryDeviceOffsets.ExtendedSize.Read(bytes);                                 // 0x1C
        ConfiguredMemorySpeed = MemoryDeviceOffsets.ConfiguredSpeed.Read(bytes);                     // 0x20
        MinimumVoltageMillivolts = MemoryDeviceOffsets.MinimumVoltage.Read(bytes);                   // 0x22
        MaximumVoltageMillivolts = MemoryDeviceOffsets.MaximumVoltage.Read(bytes);                   // 0x24
        ConfiguredVoltageMillivolts = MemoryDeviceOffsets.ConfiguredVoltage.Read(bytes);             // 0x26
        MemoryTechnologyId = MemoryDeviceOffsets.MemoryTechnology.Read(bytes);                       // 0x28
        OperatingModeCapability = MemoryDeviceOffsets.OperatingMode.Read(bytes);                     // 0x29
        FirmwareVersion = MemoryDeviceOffsets.FirmwareVersion.Read(bytes, strings);                  // 0x2B
        ModuleManufacturerId = MemoryDeviceOffsets.ModuleManufacturerId.Read(bytes);                 // 0x2C
        ModuleProductId = MemoryDeviceOffsets.ModuleProductId.Read(bytes);                           // 0x2E
        ControllerManufacturerId = MemoryDeviceOffsets.ControllerManufacturerId.Read(bytes);         // 0x30
        ControllerProductId = MemoryDeviceOffsets.ControllerProductId.Read(bytes);                   // 0x32
        NonVolatileSize = MemoryDeviceOffsets.NonVolatileSize.Read(bytes);                           // 0x34
        VolatileSize = MemoryDeviceOffsets.VolatileSize.Read(bytes);                                 // 0x3C
        CacheSize = MemoryDeviceOffsets.CacheSize.Read(bytes);                                       // 0x44
        LogicalSize = MemoryDeviceOffsets.LogicalSize.Read(bytes);                                   // 0x4C
        ExtendedSpeedMTs = MemoryDeviceOffsets.ExtendedSpeed.Read(bytes);                            // 0x54
        ExtendedConfiguredSpeedMTs = MemoryDeviceOffsets.ExtendedConfiguredSpeed.Read(bytes);        // 0x58
        Pmic0ManufacturerId = MemoryDeviceOffsets.Pmic0ManufacturerId.Read(bytes);                   // 0x5C
        Pmic0RevisionNumber = MemoryDeviceOffsets.Pmic0RevisionNumber.Read(bytes);                   // 0x5E
        RcdManufacturerId = MemoryDeviceOffsets.RcdManufacturerId.Read(bytes);                       // 0x60
        RcdRevisionNumber = MemoryDeviceOffsets.RcdRevisionNumber.Read(bytes);                       // 0x62

        // Extended Size / Configured Memory Speed were added in SMBIOS 2.7 (offset 0x1C).
        HasExtendedSize = MemoryDeviceOffsets.ExtendedSize.HasField(bytes);

        // Extended Speed / Extended Configured Memory Speed were added in SMBIOS 3.3 (offset 0x54).
        HasExtendedSpeed = MemoryDeviceOffsets.ExtendedSpeed.HasField(bytes);

        // The QWORD size fields were added in SMBIOS 3.2 (offset 0x34).
        HasNonVolatileSize = MemoryDeviceOffsets.NonVolatileSize.HasField(bytes);
        HasVolatileSize = MemoryDeviceOffsets.VolatileSize.HasField(bytes);
        HasCacheSize = MemoryDeviceOffsets.CacheSize.HasField(bytes);
        HasLogicalSize = MemoryDeviceOffsets.LogicalSize.HasField(bytes);
    }

    #region Raw properties, one per DSP0134 7.18 field, in offset order.
    public ushort Handle { get; init; }

    public ushort PhysicalMemoryArrayHandle { get; init; }

    public ushort MemoryErrorInformationHandle { get; init; }

    /// <summary>
    /// Total width in bits, including any check or error-correction bits (offset 0x08). 0xFFFF means Unknown.
    /// </summary>
    public ushort TotalWidth { get; init; }

    /// <summary>
    /// Data width in bits (offset 0x0A). 0xFFFF means Unknown.
    /// </summary>
    public ushort DataWidth { get; init; }

    /// <summary>
    /// Size in MB, or in KB when bit 15 is set (offset 0x0C). 0 means no module installed; 0xFFFF means Unknown; 0x7FFF selects <see cref="ExtendedSize"/>.
    /// </summary>
    public ushort Size { get; init; }

    /// <summary>
    /// Form factor identifier (offset 0x0E).
    /// </summary>
    public byte FormFactorId { get; init; }

    /// <summary>
    /// Device set number (offset 0x0F). 0 means not part of a set, 0xFF means Unknown.
    /// </summary>
    public byte DeviceSet { get; init; }

    /// <summary>
    /// Physically-labeled socket string (offset 0x10). Null when not provided.
    /// </summary>
    public string? DeviceLocator { get; init; }

    /// <summary>
    /// Physically-labeled bank string (offset 0x11). Null when not provided.
    /// </summary>
    public string? BankLocator { get; init; }

    /// <summary>
    /// Memory type identifier (offset 0x12).
    /// </summary>
    public byte MemoryTypeId { get; init; }

    /// <summary>
    /// Additional detail bitfield (offset 0x13). Bit 0 is reserved.
    /// </summary>
    public ushort TypeDetail { get; init; }

    /// <summary>
    /// Maximum capable speed in MT/s (offset 0x15). 0 means Unknown; 0xFFFF selects <see cref="ExtendedSpeedMTs"/>.
    /// </summary>
    public ushort Speed { get; init; }

    /// <summary>
    /// Manufacturer string (offset 0x17). Null when not provided.
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Serial number string set by the manufacturer (offset 0x18). Null when not provided.
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// Asset tag string (offset 0x19). Null when not provided.
    /// </summary>
    public string? AssetTag { get; init; }

    /// <summary>
    /// Part number string set by the manufacturer (offset 0x1A). Null when not provided.
    /// </summary>
    public string? PartNumber { get; init; }

    /// <summary>
    /// Attribute bitfield (offset 0x1B). Bits 4:0 hold the physical/package rank.
    /// </summary>
    public byte Attributes { get; init; }

    /// <summary>
    /// Extended size in MB (offset 0x1C, SMBIOS 2.7+). Bit 31 is reserved; only meaningful when <see cref="Size"/> is 0x7FFF.
    /// </summary>
    public uint ExtendedSize { get; init; }

    /// <summary>
    /// Configured speed in MT/s (offset 0x20). 0 means Unknown; 0xFFFF selects <see cref="ExtendedConfiguredSpeedMTs"/>.
    /// </summary>
    public ushort ConfiguredMemorySpeed { get; init; }

    /// <summary>
    /// Minimum operating voltage in millivolts (offset 0x22). 0 means Unknown.
    /// </summary>
    public ushort MinimumVoltageMillivolts { get; init; }

    /// <summary>
    /// Maximum operating voltage in millivolts (offset 0x24). 0 means Unknown.
    /// </summary>
    public ushort MaximumVoltageMillivolts { get; init; }

    /// <summary>
    /// Configured voltage in millivolts (offset 0x26). 0 means Unknown.
    /// </summary>
    public ushort ConfiguredVoltageMillivolts { get; init; }

    /// <summary>
    /// Memory technology type identifier (offset 0x28).
    /// </summary>
    public byte MemoryTechnologyId { get; init; }

    /// <summary>
    /// Supported operating mode bitfield (offset 0x29). 
    /// This reports supported modes, not the current mode. Bit 0 is reserved.
    /// </summary>
    public ushort OperatingModeCapability { get; init; }

    /// <summary>
    /// Firmware version string (offset 0x2B). Null when not provided.
    /// </summary>
    public string? FirmwareVersion { get; init; }

    /// <summary>
    /// Two-byte module manufacturer ID from SPD, LSB first (offset 0x2C). 0 means Unknown.
    /// </summary>
    public ushort ModuleManufacturerId { get; init; }

    /// <summary>
    /// Two-byte module product ID from SPD, LSB first (offset 0x2E). 0 means Unknown.
    /// </summary>
    public ushort ModuleProductId { get; init; }

    /// <summary>
    /// Two-byte memory subsystem controller manufacturer ID from SPD, LSB first (offset 0x30). 0 means Unknown.
    /// </summary>
    public ushort ControllerManufacturerId { get; init; }

    /// <summary>
    /// Two-byte memory subsystem controller product ID from SPD, LSB first (offset 0x32). 0 means Unknown.
    /// </summary>
    public ushort ControllerProductId { get; init; }

    /// <summary>
    /// Non-volatile portion size in bytes (offset 0x34, SMBIOS 3.2+). 0 means None; FFFFFFFFFFFFFFFFh means Unknown.
    /// </summary>
    public ulong NonVolatileSize { get; init; }

    /// <summary>
    /// Volatile portion size in bytes (offset 0x3C, SMBIOS 3.2+). 0 means None; FFFFFFFFFFFFFFFFh means Unknown.
    /// </summary>
    public ulong VolatileSize { get; init; }

    /// <summary>
    /// Cache portion size in bytes (offset 0x44, SMBIOS 3.2+). 0 means None; FFFFFFFFFFFFFFFFh means Unknown.
    /// </summary>
    public ulong CacheSize { get; init; }

    /// <summary>
    /// Logical device size in bytes (offset 0x4C, SMBIOS 3.2+). FFFFFFFFFFFFFFFFh means Unknown.
    /// </summary>
    public ulong LogicalSize { get; init; }

    /// <summary>
    /// Extended speed in MT/s (offset 0x54, SMBIOS 3.3+). Bit 31 is reserved; only meaningful when <see cref="Speed"/> is 0xFFFF.
    /// </summary>
    public uint ExtendedSpeedMTs { get; init; }

    /// <summary>
    /// Extended configured speed in MT/s (offset 0x58, SMBIOS 3.3+). Bit 31 is reserved; only meaningful when <see cref="ConfiguredMemorySpeed"/> is 0xFFFF.
    /// </summary>
    public uint ExtendedConfiguredSpeedMTs { get; init; }

    /// <summary>
    /// Two-byte PMIC0 manufacturer ID from SPD, LSB first (offset 0x5C, SMBIOS 3.7+). 0 means Unknown.
    /// </summary>
    public ushort Pmic0ManufacturerId { get; init; }

    /// <summary>
    /// PMIC0 revision number from SPD (offset 0x5E, SMBIOS 3.7+). 0xFF00 means Unknown.
    /// </summary>
    public ushort Pmic0RevisionNumber { get; init; }

    /// <summary>Two-byte RCD manufacturer ID from SPD, LSB first (offset 0x60, SMBIOS 3.7+). 0 means Unknown.</summary>
    public ushort RcdManufacturerId { get; init; }

    /// <summary>RCD revision number from SPD (offset 0x62, SMBIOS 3.7+). 0xFF00 means Unknown.</summary>
    public ushort RcdRevisionNumber { get; init; }

    #endregion

    #region Resolved properties: the final value derived from several raw fields.
    /// <summary>
    /// Installed capacity in bytes, resolved from <see cref="Size"/> and
    /// <see cref="ExtendedSize"/>. 0 means Unknown or not installed.
    /// </summary>
    public long ResolvedSizeBytes
        => DecodeSizeBytes(Size, ExtendedSize, HasExtendedSize);

    /// <summary>
    /// Maximum capable speed in MT/s, resolved from <see cref="Speed"/> and
    /// <see cref="ExtendedSpeedMTs"/>. 0 means Unknown.
    /// </summary>
    public uint ResolvedSpeedMTs
        => DecodeSpeed(Speed, ExtendedSpeedMTs, HasExtendedSpeed);

    /// <summary>
    /// Configured speed in MT/s, resolved from <see cref="ConfiguredMemorySpeed"/> and
    /// <see cref="ExtendedConfiguredSpeedMTs"/>. 0 means Unknown.
    /// </summary>
    public uint ResolvedConfiguredSpeedMTs
        => DecodeSpeed(ConfiguredMemorySpeed, ExtendedConfiguredSpeedMTs, HasExtendedSpeed);
    #endregion

    #region Derived properties: decoded from a single raw field.

    /// <summary>Total width in bits, with the Unknown sentinel (0xFFFF) normalized to 0.</summary>
    public ushort TotalWidthBits
        => NormalizeWidth(TotalWidth);

    /// <summary>Data width in bits, with the Unknown sentinel (0xFFFF) normalized to 0.</summary>
    public ushort DataWidthBits
        => NormalizeWidth(DataWidth);

    /// <summary>Physical/package rank decoded from <see cref="Attributes"/> bits 4:0. 0 means Unknown.</summary>
    public byte Rank
        => (byte) (Attributes & 0x1F);

    /// <summary>Non-volatile portion size in bytes. Null when Unknown or absent.</summary>
    public ulong? NonVolatileSizeBytes
        => HasNonVolatileSize && NonVolatileSize != ulong.MaxValue ? NonVolatileSize : null;

    /// <summary>Volatile portion size in bytes. Null when Unknown or absent.</summary>
    public ulong? VolatileSizeBytes
        => HasVolatileSize && VolatileSize != ulong.MaxValue ? VolatileSize : null;

    /// <summary>Cache portion size in bytes. Null when Unknown or absent.</summary>
    public ulong? CacheSizeBytes
        => HasCacheSize && CacheSize != ulong.MaxValue ? CacheSize : null;

    /// <summary>Logical device size in bytes. Null when Unknown or absent.</summary>
    public ulong? LogicalSizeBytes
        => HasLogicalSize && LogicalSize != ulong.MaxValue ? LogicalSize : null;

    // Human-readable names for the identifier fields.

    public string FormFactorName
        => SmbiosDataParser.GetFormFactorName(FormFactorId);

    public string MemoryTypeName
        => SmbiosDataParser.GetMemoryTypeName(MemoryTypeId);

    public string MemoryTechnologyName
        => SmbiosDataParser.GetMemoryTechnologyName(MemoryTechnologyId);

    public IReadOnlyList<string> TypeDetailNames
        => SmbiosDataParser.GetTypeDetailNames(TypeDetail);

    public IReadOnlyList<string> OperatingModeNames
        => SmbiosDataParser.GetOperatingModeNames(OperatingModeCapability);
    #endregion

    #region Helper Properties
    /// <summary>
    /// Whether Extended Size (offset 0x1C, SMBIOS 2.7+) is present.
    /// </summary>
    public bool HasExtendedSize { get; init; }

    /// <summary>
    /// Whether Extended Speed / Extended Configured Memory Speed (offsets 0x54/0x58, SMBIOS 3.3+) are present.
    /// </summary>
    public bool HasExtendedSpeed { get; init; }

    /// <summary>
    /// Whether Non-Volatile Size (offset 0x34, SMBIOS 3.2+) is present.
    /// </summary>
    public bool HasNonVolatileSize { get; init; }

    /// <summary>
    /// Whether Volatile Size (offset 0x3C, SMBIOS 3.2+) is present.
    /// </summary>
    public bool HasVolatileSize { get; init; }

    /// <summary>
    /// Whether Cache Size (offset 0x44, SMBIOS 3.2+) is present.
    /// </summary>
    public bool HasCacheSize { get; init; }

    /// <summary>
    /// Whether Logical Size (offset 0x4C, SMBIOS 3.2+) is present.
    /// </summary>
    public bool HasLogicalSize { get; init; }
    #endregion

    /// <summary>
    /// Whether the slot has an installed module (Size is not 0). Empty slots are skipped.
    /// </summary>
    internal static bool HasModule(ReadOnlySpan<byte> bytes)
        => MemoryDeviceOffsets.Size.Read(bytes) != 0;

    private static long DecodeSizeBytes(ushort size, uint extendedSizeMb, bool hasExtendedSize)
    {
        const long KB = 1024L;
        const long MB = 1024L * 1024;
        if (size == 0 || size == 0xFFFF)
            return 0; // No Module Installed, or Unknown

        if (size == 0x7FFF && hasExtendedSize)
            return (extendedSizeMb & 0x7FFFFFFFu) * MB; // Extended Size is in MB

        if ((size & 0x8000) != 0)
            return (size & 0x7FFF) * KB; // Bit 15 set: value is in KB

        return size * MB; // Otherwise the value is in MB
    }

    private static ushort NormalizeWidth(ushort width)
        => width == 0xFFFF ? (ushort) 0 : width; // 0xFFFF and 0 both mean Unknown

    private static uint DecodeSpeed(ushort speed, uint extendedSpeed, bool hasExtendedSpeed)
    {
        // Speed is in MT/s. 0xFFFF selects the extended speed field (SMBIOS 3.3+); 0 means Unknown.
        // Bit 31 of the extended field is reserved.
        if (speed == 0xFFFF)
            return hasExtendedSpeed ? (extendedSpeed & 0x7FFFFFFF) : 0;
        return speed;
    }

    /// <summary>
    /// Returns a human-readable single-line summary of this memory device.
    /// Properties whose value is Unknown (missing strings or 0) are omitted.
    /// </summary>
    public string ToDisplayString()
    {
        List<string> values = new();

        string? locator = DeviceLocator.IsBlank() ? null : DeviceLocator.Trim(); // 0x10
        string? bank = BankLocator.IsBlank() ? null : BankLocator.Trim();        // 0x11

        string? sizeAndType = null;
        if (ResolvedSizeBytes > 0)
        {
            sizeAndType = ResolvedSizeBytes.ToDisplaySize();
            if (MemoryTypeId != 0 && MemoryTypeName != "Unknown")
                sizeAndType += $" {MemoryTypeName}"; // 0x12
            if (FormFactorId != 0 && FormFactorName != "Unknown")
                sizeAndType += $" {FormFactorName}"; // 0x0E
        }

        if (locator is not null || bank is not null)
        {
            string segment = locator is not null && bank is not null and not "None"
                ? $"{locator} ({bank})"
                : locator ?? bank ?? "";
            if (sizeAndType is not null)
                segment += $": {sizeAndType}";
            values.Add(segment);
        }
        else if (sizeAndType is not null)
        {
            values.Add(sizeAndType);
        }

        if (ResolvedSpeedMTs > 0)
        {
            string segment = $"Speed {ResolvedSpeedMTs.ToDisplayMemorySpeed()}"; // 0x15
            if (ResolvedConfiguredSpeedMTs > 0)
                segment += $" (Configured {ResolvedConfiguredSpeedMTs.ToDisplayMemorySpeed()})"; // 0x20
            values.Add(segment);
        }

        string? manufacturer = Manufacturer.IsBlank() ? null : Manufacturer.Trim(); // 0x17
        string? part = PartNumber.IsBlank() ? null : PartNumber.Trim();             // 0x1A
        if (manufacturer is not null && part is not null)
            values.Add($"{manufacturer}, Part {part}");
        else if (manufacturer is not null)
            values.Add(manufacturer);
        else if (part is not null and not "None")
            values.Add($"Part {part}");

        if (Rank > 0)
            values.Add(Rank.ToDisplayRank()); // 0x1B

        return string.Join(", ", values);
    }

    public override string ToString()
        => ToDisplayString();


    /// <summary>
    /// Type 17 field offsets (DSP0134 7.18, Memory Device).
    /// Each offset is bundled with its data width.
    /// </summary>
    private static class MemoryDeviceOffsets
    {
#pragma warning disable format
        internal static readonly UInt16Field Handle                       = new(0x02);
        internal static readonly UInt16Field PhysicalMemoryArrayHandle    = new(0x04);
        internal static readonly UInt16Field MemoryErrorInformationHandle = new(0x06);
        internal static readonly UInt16Field TotalWidth                   = new(0x08);
        internal static readonly UInt16Field DataWidth                    = new(0x0A);
        internal static readonly UInt16Field Size                         = new(0x0C);
        internal static readonly ByteField   FormFactor                   = new(0x0E);
        internal static readonly ByteField   DeviceSet                    = new(0x0F);
        internal static readonly StringField DeviceLocator                = new(0x10);
        internal static readonly StringField BankLocator                  = new(0x11);
        internal static readonly ByteField   MemoryType                   = new(0x12);
        internal static readonly UInt16Field TypeDetail                   = new(0x13);
        internal static readonly UInt16Field Speed                        = new(0x15);
        internal static readonly StringField Manufacturer                 = new(0x17);
        internal static readonly StringField SerialNumber                 = new(0x18);
        internal static readonly StringField AssetTag                     = new(0x19);
        internal static readonly StringField PartNumber                   = new(0x1A);
        internal static readonly ByteField   Attributes                   = new(0x1B);
        internal static readonly UInt32Field ExtendedSize                 = new(0x1C);
        internal static readonly UInt16Field ConfiguredSpeed              = new(0x20);
        internal static readonly UInt16Field MinimumVoltage               = new(0x22);
        internal static readonly UInt16Field MaximumVoltage               = new(0x24);
        internal static readonly UInt16Field ConfiguredVoltage            = new(0x26);
        internal static readonly ByteField   MemoryTechnology             = new(0x28);
        internal static readonly UInt16Field OperatingMode                = new(0x29);
        internal static readonly StringField FirmwareVersion              = new(0x2B);
        internal static readonly UInt16Field ModuleManufacturerId         = new(0x2C);
        internal static readonly UInt16Field ModuleProductId              = new(0x2E);
        internal static readonly UInt16Field ControllerManufacturerId     = new(0x30);
        internal static readonly UInt16Field ControllerProductId          = new(0x32);
        internal static readonly UInt64Field NonVolatileSize              = new(0x34);
        internal static readonly UInt64Field VolatileSize                 = new(0x3C);
        internal static readonly UInt64Field CacheSize                    = new(0x44);
        internal static readonly UInt64Field LogicalSize                  = new(0x4C);
        internal static readonly UInt32Field ExtendedSpeed                = new(0x54);
        internal static readonly UInt32Field ExtendedConfiguredSpeed      = new(0x58);
        internal static readonly UInt16Field Pmic0ManufacturerId          = new(0x5C);
        internal static readonly UInt16Field Pmic0RevisionNumber          = new(0x5E, 0xFF00);
        internal static readonly UInt16Field RcdManufacturerId            = new(0x60);
        internal static readonly UInt16Field RcdRevisionNumber            = new(0x62, 0xFF00);
#pragma warning restore format
    }
}
