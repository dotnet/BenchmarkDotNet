using BenchmarkDotNet.Detectors.Smbios;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

internal static partial class SmbiosTables
{
    /// <summary>
    /// Starts building a Type 4 (Processor Information) structure with zeroed fields.
    /// Use it when a test needs the Unknown baseline.
    /// </summary>
    internal static Type4Builder Type4() => new();

    /// <summary>
    /// Starts building a Type 4 (Processor Information) structure with default values.
    /// </summary>
    internal static Type4Builder CreateDefaultProcessorBuilder()
        => Type4()
            .WithSocket(ProcessorDefaults.Socket)
            .WithSocketType(ProcessorDefaults.SocketType)
            .WithManufacturer(ProcessorDefaults.Manufacturer)
            .WithVersion(ProcessorDefaults.Version)
            .WithProcessorId(ProcessorDefaults.ProcessorId)
            .WithExternalClockMHz(ProcessorDefaults.ExternalClockMHz)
            .WithMaxSpeedMHz(ProcessorDefaults.MaxSpeedMHz)
            .WithCurrentSpeedMHz(ProcessorDefaults.CurrentSpeedMHz)
            .WithStatus(ProcessorDefaults.Status)
            .WithCoreCount(ProcessorDefaults.CoreCount)
            .WithCoreEnabled(ProcessorDefaults.CoreEnabled)
            .WithThreadCount(ProcessorDefaults.ThreadCount)
            .WithThreadEnabled(ProcessorDefaults.ThreadEnabled);

    internal enum Status : byte
    {
        Default = 0x41, // 0x40: CPU Socket Populated & 0x01: CPU Enabled
        UnPopulated = 0x00, // 0x40 is clear, so the parser skips the socket.
        PopulatedNotEnabled = 0x40, // Populated, but the CPU status is 0 (Unknown)
    }

    /// <summary>
    /// Fluent builder for a synthetic Type 4 (Processor Information) structure and its string-set.
    /// Every field of the formatted area is configurable; Type and Length are fixed by the structure.
    /// </summary>
    internal sealed class Type4Builder
    {
        private const byte Length = 0x33; // This value is based on SMBIOS specification 3.10.0

        private ushort handle;
        private string? socket;
        private byte processorType;
        private byte processorFamily;
        private string? manufacturer;
        private byte[] processorId = new byte[8]; // Fixed width; all-zero means the processor reports no ID.
        private string? version;
        private byte voltage;
        private ushort externalClockMHz;
        private ushort maxSpeedMHz;
        private ushort currentSpeedMHz;
        private byte status;
        private byte processorUpgrade;
        private ushort l1CacheHandle;
        private ushort l2CacheHandle;
        private ushort l3CacheHandle;
        private string? serialNumber;
        private string? assetTag;
        private string? partNumber;
        private byte coreCount;
        private byte coreEnabled;
        private byte threadCount;
        private ushort processorCharacteristics;
        private ushort processorFamily2;
        private ushort coreCount2;
        private ushort coreEnabled2;
        private ushort threadCount2;
        private ushort threadEnabled;
        private string? socketType;

        internal Type4Builder WithHandle(ushort value)
            => Set(ref handle, value);

        internal Type4Builder WithSocket(string? value)
            => Set(ref socket, value);

        internal Type4Builder WithProcessorType(byte value)
            => Set(ref processorType, value);

        internal Type4Builder WithProcessorFamily(byte value)
            => Set(ref processorFamily, value);

        internal Type4Builder WithManufacturer(string? value)
            => Set(ref manufacturer, value);

        internal Type4Builder WithProcessorId(byte[] value)
        {
            if (value.Length != 8)
                throw new ArgumentException("Processor ID must be exactly 8 bytes.", nameof(value));

            return Set(ref processorId, value);
        }

        internal Type4Builder WithVersion(string? value)
            => Set(ref version, value);

        internal Type4Builder WithVoltage(byte value)
            => Set(ref voltage, value);

        internal Type4Builder WithExternalClockMHz(ushort value)
            => Set(ref externalClockMHz, value);

        internal Type4Builder WithMaxSpeedMHz(ushort value)
            => Set(ref maxSpeedMHz, value);

        internal Type4Builder WithCurrentSpeedMHz(ushort value)
            => Set(ref currentSpeedMHz, value);

        internal Type4Builder WithStatus(byte value)
            => Set(ref status, value);

        internal Type4Builder WithStatus(Status value)
            => Set(ref status, (byte) value);

        internal Type4Builder WithProcessorUpgrade(byte value)
            => Set(ref processorUpgrade, value);

        internal Type4Builder WithL1CacheHandle(ushort value)
            => Set(ref l1CacheHandle, value);

        internal Type4Builder WithL2CacheHandle(ushort value)
            => Set(ref l2CacheHandle, value);

        internal Type4Builder WithL3CacheHandle(ushort value)
            => Set(ref l3CacheHandle, value);

        internal Type4Builder WithSerialNumber(string? value)
            => Set(ref serialNumber, value);

        internal Type4Builder WithAssetTag(string? value)
            => Set(ref assetTag, value);

        internal Type4Builder WithPartNumber(string? value)
            => Set(ref partNumber, value);

        internal Type4Builder WithCoreCount(byte value)
            => Set(ref coreCount, value);

        internal Type4Builder WithCoreEnabled(byte value)
            => Set(ref coreEnabled, value);

        internal Type4Builder WithThreadCount(byte value)
            => Set(ref threadCount, value);

        internal Type4Builder WithProcessorCharacteristics(ushort value)
            => Set(ref processorCharacteristics, value);

        internal Type4Builder WithProcessorFamily2(ushort value)
            => Set(ref processorFamily2, value);

        internal Type4Builder WithCoreCount2(ushort value)
            => Set(ref coreCount2, value);

        internal Type4Builder WithCoreEnabled2(ushort value)
            => Set(ref coreEnabled2, value);

        internal Type4Builder WithThreadCount2(ushort value)
            => Set(ref threadCount2, value);

        internal Type4Builder WithThreadEnabled(ushort value)
            => Set(ref threadEnabled, value);

        internal Type4Builder WithSocketType(string? value)
            => Set(ref socketType, value);

        // Assigns the value and returns this builder for chaining.
        private Type4Builder Set<T>(ref T field, T value)
        {
            field = value;
            return this;
        }

        internal byte[] Build()
        {
            var builder = new SmbiosTableBuilder(SmbiosType.ProcessorInformation, Length);

            builder.Add(handle);                   // 0x02-0x03: Handle
            builder.Add(socket);                   // 0x04: Socket Designation
            builder.Add(processorType);            // 0x05: Processor Type
            builder.Add(processorFamily);          // 0x06: Processor Family
            builder.Add(manufacturer);             // 0x07: Manufacturer
            builder.Add(processorId);              // 0x08-0x0F: Processor ID
            builder.Add(version);                  // 0x10: Processor Version
            builder.Add(voltage);                  // 0x11: Voltage
            builder.Add(externalClockMHz);         // 0x12-0x13: External Clock
            builder.Add(maxSpeedMHz);              // 0x14-0x15: Max Speed
            builder.Add(currentSpeedMHz);          // 0x16-0x17: Current Speed
            builder.Add(status);                   // 0x18: Status
            builder.Add(processorUpgrade);         // 0x19: Processor Upgrade
            builder.Add(l1CacheHandle);            // 0x1A-0x1B: L1 Cache Handle
            builder.Add(l2CacheHandle);            // 0x1C-0x1D: L2 Cache Handle
            builder.Add(l3CacheHandle);            // 0x1E-0x1F: L3 Cache Handle
            builder.Add(serialNumber);             // 0x20: Serial Number
            builder.Add(assetTag);                 // 0x21: Asset Tag
            builder.Add(partNumber);               // 0x22: Part Number
            builder.Add(coreCount);                // 0x23: Core Count
            builder.Add(coreEnabled);              // 0x24: Core Enabled
            builder.Add(threadCount);              // 0x25: Thread Count
            builder.Add(processorCharacteristics); // 0x26-0x27: Processor Characteristics
            builder.Add(processorFamily2);         // 0x28-0x29: Processor Family 2
            builder.Add(coreCount2);               // 0x2A-0x2B: Core Count 2
            builder.Add(coreEnabled2);             // 0x2C-0x2D: Core Enabled 2
            builder.Add(threadCount2);             // 0x2E-0x2F: Thread Count 2
            builder.Add(threadEnabled);            // 0x30-0x31: Thread Enabled (3.6+)
            builder.Add(socketType);               // 0x32: Socket Type (3.8+)

            return builder.GetBytes();
        }

        /// <summary>
        /// Builds the structure and parses it back into a <see cref="SmbiosProcessorInfo"/>.
        /// </summary>
        internal SmbiosProcessorInfo BuildProcessorInfo()
            => SmbiosDataParser.ParseProcessorInfoTable(Build())[0];
    }

    /// <summary>
    /// Field values shared by the Type 4 (Processor Information) test cases.
    /// Tests override only the fields that their scenario is about.
    /// </summary>
    internal static class ProcessorDefaults
    {
        internal const string Socket = "AM4";
        internal const string SocketType = "Socket AM4";
        internal const string Manufacturer = "Advanced Micro Devices, Inc.";
        internal const string Version = "AMD Ryzen 5 3500 6-Core Processor";

        // Processor ID format bytes depend on the CPU architecture (x86/arm64).
        internal static readonly byte[] ProcessorId =
        [
            0x17, 0x8B, 0xFB, 0xFF, // CPU Feature flags
            0x00, 0x87, 0x0F, 0x10, // CPU Family/Model/Stepping
        ];
        internal static readonly string ProcessorIdString = BitConverter.ToString(ProcessorId).Replace("-", "");

        internal const ushort ExternalClockMHz = 100;
        internal const ushort MaxSpeedMHz = 4100;
        internal const ushort CurrentSpeedMHz = 3600;
        internal const byte Status = (byte) SmbiosTables.Status.Default;
        internal const byte CoreCount = 6;
        internal const byte CoreEnabled = 6;
        internal const byte ThreadCount = 6;
        internal const ushort ThreadEnabled = 6;
    }
}
