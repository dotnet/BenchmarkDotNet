using BenchmarkDotNet.Detectors.Smbios;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

internal static partial class SmbiosTables
{
    /// <summary>
    /// Starts building a Type 17 (Memory Device) structure with zeroed fields.
    /// Use it when a test needs the Unknown baseline.
    /// </summary>
    internal static Type17Builder Type17() => new();

    /// <summary>
    /// Starts building a Type 17 (Memory Device) structure pre-populated with default values.
    /// </summary>
    internal static Type17Builder CreateDefaultMemoryDeviceBuilder()
        => Type17()
            .WithSize(MemoryDeviceDefaults.Size)
            .WithFormFactor(MemoryDeviceDefaults.FormFactor)
            .WithDeviceLocator(MemoryDeviceDefaults.DeviceLocator)
            .WithBankLocator(MemoryDeviceDefaults.BankLocator)
            .WithMemoryType(MemoryDeviceDefaults.MemoryType)
            .WithSpeed(MemoryDeviceDefaults.Speed)
            .WithManufacturer(MemoryDeviceDefaults.Manufacturer)
            .WithSerialNumber(MemoryDeviceDefaults.SerialNumber)
            .WithAssetTag(MemoryDeviceDefaults.AssetTag)
            .WithPartNumber(MemoryDeviceDefaults.PartNumber)
            .WithRank(MemoryDeviceDefaults.Rank)
            .WithConfiguredSpeed(MemoryDeviceDefaults.ConfiguredSpeed)
            .WithFirmwareVersion(MemoryDeviceDefaults.FirmwareVersion)
            .WithModuleManufacturerId(MemoryDeviceDefaults.ModuleManufacturerId)
            .WithModuleProductId(MemoryDeviceDefaults.ModuleProductId)
            .WithControllerManufacturerId(MemoryDeviceDefaults.ControllerManufacturerId)
            .WithControllerProductId(MemoryDeviceDefaults.ControllerProductId)
            .WithPmic0ManufacturerId(MemoryDeviceDefaults.Pmic0ManufacturerId)
            .WithPmic0Revision(MemoryDeviceDefaults.Pmic0Revision)
            .WithRcdManufacturerId(MemoryDeviceDefaults.RcdManufacturerId)
            .WithRcdRevision(MemoryDeviceDefaults.RcdRevision);

    /// <summary>
    /// Fluent builder for a synthetic Type 17 (Memory Device) structure and its string-set.
    /// Every field of the formatted area is configurable; Type and Length are fixed by the structure.
    /// Note that Size = 0 means "no module installed", so callers must set a size for the structure to be parsed.
    /// </summary>
    internal sealed class Type17Builder
    {
        private const byte Length = 0x64;

        private ushort handle;
        private ushort physicalMemoryArrayHandle;
        private ushort memoryErrorInformationHandle;
        private ushort totalWidth;
        private ushort dataWidth;
        private ushort size;
        private byte formFactor;
        private byte deviceSet;
        private string? deviceLocator;
        private string? bankLocator;
        private byte memoryType;
        private ushort typeDetail;
        private ushort speed;
        private string? manufacturer;
        private string? serialNumber;
        private string? assetTag;
        private string? partNumber;
        private byte rank;
        private uint extendedSizeMb;
        private ushort configuredSpeed;
        private ushort minVoltage;
        private ushort maxVoltage;
        private ushort configuredVoltage;
        private byte technology;
        private ushort operatingMode;
        private string? firmwareVersion;
        private ushort moduleManufacturerId;
        private ushort moduleProductId;
        private ushort controllerManufacturerId;
        private ushort controllerProductId;
        private ulong nonVolatileSize;
        private ulong volatileSize;
        private ulong cacheSize;
        private ulong logicalSize;
        private uint extendedSpeedMTs;
        private uint extendedConfiguredSpeedMTs;
        private ushort pmic0ManufacturerId;
        private ushort pmic0Revision;
        private ushort rcdManufacturerId;
        private ushort rcdRevision;

        internal Type17Builder WithHandle(ushort value)
            => Set(ref handle, value);

        internal Type17Builder WithPhysicalMemoryArrayHandle(ushort value)
            => Set(ref physicalMemoryArrayHandle, value);

        internal Type17Builder WithMemoryErrorInformationHandle(ushort value)
            => Set(ref memoryErrorInformationHandle, value);

        internal Type17Builder WithTotalWidth(ushort value)
            => Set(ref totalWidth, value);

        internal Type17Builder WithDataWidth(ushort value)
            => Set(ref dataWidth, value);

        internal Type17Builder WithSize(ushort value)
            => Set(ref size, value);

        internal Type17Builder WithFormFactor(byte value)
            => Set(ref formFactor, value);

        internal Type17Builder WithDeviceSet(byte value)
            => Set(ref deviceSet, value);

        internal Type17Builder WithDeviceLocator(string? value)
            => Set(ref deviceLocator, value);

        internal Type17Builder WithBankLocator(string? value)
            => Set(ref bankLocator, value);

        internal Type17Builder WithMemoryType(byte value)
            => Set(ref memoryType, value);

        internal Type17Builder WithTypeDetail(ushort value)
            => Set(ref typeDetail, value);

        internal Type17Builder WithSpeed(ushort value)
            => Set(ref speed, value);

        internal Type17Builder WithManufacturer(string? value)
            => Set(ref manufacturer, value);

        internal Type17Builder WithSerialNumber(string? value)
            => Set(ref serialNumber, value);

        internal Type17Builder WithAssetTag(string? value)
            => Set(ref assetTag, value);

        internal Type17Builder WithPartNumber(string? value)
            => Set(ref partNumber, value);

        internal Type17Builder WithRank(byte value)
            => Set(ref rank, value);

        internal Type17Builder WithExtendedSizeMb(uint value)
            => Set(ref extendedSizeMb, value);

        internal Type17Builder WithConfiguredSpeed(ushort value)
            => Set(ref configuredSpeed, value);

        internal Type17Builder WithMinVoltage(ushort value)
            => Set(ref minVoltage, value);

        internal Type17Builder WithMaxVoltage(ushort value)
            => Set(ref maxVoltage, value);

        internal Type17Builder WithConfiguredVoltage(ushort value)
            => Set(ref configuredVoltage, value);

        internal Type17Builder WithTechnology(byte value)
            => Set(ref technology, value);

        internal Type17Builder WithOperatingMode(ushort value)
            => Set(ref operatingMode, value);

        internal Type17Builder WithFirmwareVersion(string? value)
            => Set(ref firmwareVersion, value);

        internal Type17Builder WithModuleManufacturerId(ushort value)
            => Set(ref moduleManufacturerId, value);

        internal Type17Builder WithModuleProductId(ushort value)
            => Set(ref moduleProductId, value);

        internal Type17Builder WithControllerManufacturerId(ushort value)
            => Set(ref controllerManufacturerId, value);

        internal Type17Builder WithControllerProductId(ushort value)
            => Set(ref controllerProductId, value);

        internal Type17Builder WithNonVolatileSize(ulong value)
            => Set(ref nonVolatileSize, value);

        internal Type17Builder WithVolatileSize(ulong value)
            => Set(ref volatileSize, value);

        internal Type17Builder WithCacheSize(ulong value)
            => Set(ref cacheSize, value);

        internal Type17Builder WithLogicalSize(ulong value)
            => Set(ref logicalSize, value);

        internal Type17Builder WithExtendedSpeedMTs(uint value)
            => Set(ref extendedSpeedMTs, value);

        internal Type17Builder WithExtendedConfiguredSpeedMTs(uint value)
            => Set(ref extendedConfiguredSpeedMTs, value);

        internal Type17Builder WithPmic0ManufacturerId(ushort value)
            => Set(ref pmic0ManufacturerId, value);

        internal Type17Builder WithPmic0Revision(ushort value)
            => Set(ref pmic0Revision, value);

        internal Type17Builder WithRcdManufacturerId(ushort value)
            => Set(ref rcdManufacturerId, value);

        internal Type17Builder WithRcdRevision(ushort value)
            => Set(ref rcdRevision, value);

        // Assigns the value and returns this builder for chaining.
        private Type17Builder Set<T>(ref T field, T value)
        {
            field = value;
            return this;
        }

        internal byte[] Build()
        {
            var builder = new SmbiosTableBuilder(SmbiosType.MemoryDevice, Length);

            builder.Add(handle);                       // 0x02-0x03: Handle
            builder.Add(physicalMemoryArrayHandle);    // 0x04-0x05: Physical Memory Array Handle
            builder.Add(memoryErrorInformationHandle); // 0x06-0x07: Memory Error Information Handle
            builder.Add(totalWidth);                   // 0x08-0x09: Total Width
            builder.Add(dataWidth);                    // 0x0A-0x0B: Data Width
            builder.Add(size);                         // 0x0C-0x0D: Size
            builder.Add(formFactor);                   // 0x0E: Form Factor
            builder.Add(deviceSet);                    // 0x0F: Device Set
            builder.Add(deviceLocator);                // 0x10: Device Locator
            builder.Add(bankLocator);                  // 0x11: Bank Locator
            builder.Add(memoryType);                   // 0x12: Memory Type
            builder.Add(typeDetail);                   // 0x13-0x14: Type Detail
            builder.Add(speed);                        // 0x15-0x16: Speed
            builder.Add(manufacturer);                 // 0x17: Manufacturer
            builder.Add(serialNumber);                 // 0x18: Serial Number
            builder.Add(assetTag);                     // 0x19: Asset Tag
            builder.Add(partNumber);                   // 0x1A: Part Number
            builder.Add((byte) (rank & 0x1F));         // 0x1B: Attributes (Rank in bits 4:0)
            builder.Add(extendedSizeMb);               // 0x1C-0x1F: Extended Size
            builder.Add(configuredSpeed);              // 0x20-0x21: Configured Memory Speed
            builder.Add(minVoltage);                   // 0x22-0x23: Minimum Voltage
            builder.Add(maxVoltage);                   // 0x24-0x25: Maximum Voltage
            builder.Add(configuredVoltage);            // 0x26-0x27: Configured Voltage
            builder.Add(technology);                   // 0x28: Memory Technology
            builder.Add(operatingMode);                // 0x29-0x2A: Memory Operating Mode Capability
            builder.Add(firmwareVersion);              // 0x2B: Firmware Version
            builder.Add(moduleManufacturerId);         // 0x2C-0x2D: Module Manufacturer ID
            builder.Add(moduleProductId);              // 0x2E-0x2F: Module Product ID
            builder.Add(controllerManufacturerId);     // 0x30-0x31: Controller Manufacturer ID
            builder.Add(controllerProductId);          // 0x32-0x33: Controller Product ID
            builder.Add(nonVolatileSize);              // 0x34-0x3B: Non-Volatile Size
            builder.Add(volatileSize);                 // 0x3C-0x43: Volatile Size
            builder.Add(cacheSize);                    // 0x44-0x4B: Cache Size
            builder.Add(logicalSize);                  // 0x4C-0x53: Logical Size
            builder.Add(extendedSpeedMTs);             // 0x54-0x57: Extended Speed
            builder.Add(extendedConfiguredSpeedMTs);   // 0x58-0x5B: Extended Configured Speed
            builder.Add(pmic0ManufacturerId);          // 0x5C-0x5D: PMIC0 Manufacturer ID (3.7+)
            builder.Add(pmic0Revision);                // 0x5E-0x5F: PMIC0 Revision Number (3.7+)
            builder.Add(rcdManufacturerId);            // 0x60-0x61: RCD Manufacturer ID (3.7+)
            builder.Add(rcdRevision);                  // 0x62-0x63: RCD Revision Number (3.7+)

            return builder.GetBytes();
        }

        /// <summary>Builds the structure and parses it back into a <see cref="SmbiosMemoryDeviceInfo"/>.</summary>
        internal SmbiosMemoryDeviceInfo BuildMemoryDevice()
            => SmbiosDataParser.ParseMemoryDevicesInfo(Build())[0];
    }

    /// <summary>
    /// Field values shared by the Type 17 (Memory Device) test cases.
    /// Tests override only the fields that their scenario is about.
    /// </summary>
    internal static class MemoryDeviceDefaults
    {
        internal const ushort Size = 8192; // MB, 8 GB module.
        internal const byte FormFactor = 0x09; // DIMM
        internal const string DeviceLocator = "DIMM A1";
        internal const string BankLocator = "BANK 0";
        internal const byte MemoryType = 0x1A; // DDR4
        internal const ushort Speed = 3200;
        internal const string Manufacturer = "Samsung";
        internal const string SerialNumber = "TESTSERIAL"; // Note: Don't embed real serial numbers.
        internal const string PartNumber = "M378A1K43CB2-CRC";
        internal const byte Rank = 2;
        internal const ushort ConfiguredSpeed = 3200;

        // SPD module / controller IDs (SMBIOS 3.0+).
        internal const ushort ModuleManufacturerId = 0x02CE;
        internal const ushort ModuleProductId = 0x1234;
        internal const ushort ControllerManufacturerId = 0x00B3;
        internal const ushort ControllerProductId = 0x5678;

        // PMIC / RCD IDs (SMBIOS 3.7+).
        internal const ushort Pmic0ManufacturerId = 0x02CE;
        internal const ushort Pmic0Revision = 0x0012;
        internal const ushort RcdManufacturerId = 0x00B3;
        internal const ushort RcdRevision = 0x0034;

        internal const string FirmwareVersion = "01";
        internal const string AssetTag = "ATAG0123";
    }
}
