namespace BenchmarkDotNet.Detectors.Smbios;

internal partial class SmbiosDataParser
{
    /// <summary>
    /// Gets memory device information (SMBIOS Type 17) for every populated slot.
    /// Slots without an installed module (Size = 0) are skipped.
    /// </summary>
    public static IReadOnlyList<SmbiosMemoryDeviceInfo> ParseMemoryDevicesInfo(ReadOnlySpan<byte> data)
    {
        var results = new List<SmbiosMemoryDeviceInfo>();
        var items = EnumerateStructures(data).Where(x => x.Type == SmbiosType.MemoryDevice);
        foreach (SmbiosStructure item in items)
        {
            // Gets structure's formatted area (fixed-length fields).
            ReadOnlySpan<byte> bytes = data.Slice(item.Offset, item.Length);

            // Skip empty memory slot.
            if (!SmbiosMemoryDeviceInfo.HasModule(bytes))
                continue;

            // Read the structure's string-set (unformatted area).
            var strings = ReadStringSet(data, item.StringSetStart, item.NextOffset);

            results.Add(new SmbiosMemoryDeviceInfo(bytes, strings));
        }

        return results;
    }

    // DSP0134 7.18.1, Memory Device Form Factor.
    internal static string GetFormFactorName(byte id) => id switch
    {
        0x00 => "Reserved",
        0x01 => "Other",
        0x02 => "Unknown",
        0x03 => "SIMM",
        0x04 => "SIP",
        0x05 => "Chip",
        0x06 => "DIP",
        0x07 => "ZIP",
        0x08 => "Proprietary Card",
        0x09 => "DIMM",
        0x0A => "TSOP",
        0x0B => "Row Of Chips",
        0x0C => "RIMM",
        0x0D => "SODIMM",
        0x0E => "SRIMM",
        0x0F => "FB-DIMM",
        0x10 => "Die",
        0x11 => "CAMM",
        0x12 => "CUDIMM",
        0x13 => "CSODIMM",
        _ => $"Unknown (0x{id:X2})"
    };

    // DSP0134 7.18.2, Memory Device Type. Values above 0x26 are out of spec.
    internal static string GetMemoryTypeName(byte id) => id switch
    {
        0x00 => "Reserved",
        0x01 => "Other",
        0x02 => "Unknown",
        0x03 => "DRAM",
        0x04 => "EDRAM",
        0x05 => "VRAM",
        0x06 => "SRAM",
        0x07 => "RAM",
        0x08 => "ROM",
        0x09 => "Flash",
        0x0A => "EEPROM",
        0x0B => "FEPROM",
        0x0C => "EPROM",
        0x0D => "CDRAM",
        0x0E => "3DRAM",
        0x0F => "SDRAM",
        0x10 => "SGRAM",
        0x11 => "RDRAM",
        0x12 => "DDR",
        0x13 => "DDR2",
        0x14 => "DDR2 FB-DIMM",
        0x15 => "Reserved",
        0x16 => "Reserved",
        0x17 => "Reserved",
        0x18 => "DDR3",
        0x19 => "FBD2",
        0x1A => "DDR4",
        0x1B => "LPDDR",
        0x1C => "LPDDR2",
        0x1D => "LPDDR3",
        0x1E => "LPDDR4",
        0x1F => "Logical non-volatile device",
        0x20 => "HBM",
        0x21 => "HBM2",
        0x22 => "DDR5",
        0x23 => "LPDDR5",
        0x24 => "HBM3",
        0x25 => "MRDIMM",
        0x26 => "LPDDR6",
        _ => $"Unknown (0x{id:X2})"
    };

    // DSP0134 7.18.3, Memory Device Type Detail (bitfield, bit 0 is reserved).
    internal static IReadOnlyList<string> GetTypeDetailNames(ushort detail)
    {
        string[] names =
        [
            "Other",
            "Unknown",
            "Fast-paged",
            "Static Column",
            "Pseudo-static",
            "RAMBus",
            "Synchronous",
            "CMOS",
            "EDO",
            "Window DRAM",
            "Cache DRAM",
            "Non-Volatile",
            "Registered (Buffered)",
            "Unbuffered (Unregistered)",
            "LRDIMM",
        ];

        var result = new List<string>();
        for (int bit = 1; bit <= 15; bit++)
        {
            if ((detail & (1 << bit)) != 0)
                result.Add(names[bit - 1]);
        }

        return result;
    }

    // DSP0134 7.18.6, Memory Technology.
    internal static string GetMemoryTechnologyName(byte id) => id switch
    {
        0x00 => "Reserved",
        0x01 => "Other",
        0x02 => "Unknown",
        0x03 => "DRAM",
        0x04 => "NVDIMM-N",
        0x05 => "NVDIMM-F",
        0x06 => "NVDIMM-P",
        0x07 => "Intel Optane DC persistent memory",
        0x08 => "MRDIMM", // Deprecated
        _ => $"Unknown (0x{id:X2})"
    };

    // DSP0134 7.18.7, Memory Operating Mode Capability (bitfield, bit 0 is reserved).
    internal static IReadOnlyList<string> GetOperatingModeNames(ushort capability)
    {
        string[] names =
        [
            "Other",
            "Unknown",
            "Volatile memory",
            "Byte-accessible persistent memory",
            "Block-accessible persistent memory",
        ];
        var result = new List<string>();
        for (int bit = 1; bit <= 5; bit++)
        {
            if ((capability & (1 << bit)) != 0)
                result.Add(names[bit - 1]);
        }

        return result;
    }
}
