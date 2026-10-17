using AwesomeAssertions;
using BenchmarkDotNet.Environments;
using System.Buffers.Binary;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class PhysicalMemoryInfoTests
    {
        private const long SixteenGb = 16L * 1024 * 1024 * 1024;
        private const long EightGb = 8L * 1024 * 1024 * 1024;
        private const long TwoHundredFiftySixMb = 256L * 1024 * 1024;

        private const byte Ddr4 = 0x1A;
        private const byte Ddr5 = 0x22;
        private const byte UnknownType = 0x02;

        // Structure lengths per SMBIOS version (DSP0134).
        private const byte Smbios23Length = 0x1B;
        private const byte Smbios28Length = 0x28;
        private const byte Smbios33Length = 0x5C;

        [Theory]
        [InlineData(null, null, null, "16 GB")]
        [InlineData(EightGb, null, null, "16 GB Total, 8 GB Available")]
        [InlineData(null, 6400L, "DDR5", "16 GB (DDR5-6400MT/s)")]
        [InlineData(EightGb, 6400L, "DDR5", "16 GB Total (DDR5-6400MT/s), 8 GB Available")]
        [InlineData(EightGb, null, "LPDDR5", "16 GB Total (LPDDR5), 8 GB Available")]
        [InlineData(EightGb, 3200L, null, "16 GB Total (3200MT/s), 8 GB Available")]
        public void ToFormattedString(long? available, long? speed, string? type, string expected)
        {
            var info = new PhysicalMemoryInfo(SixteenGb, available, speed, type);
            Assert.Equal(expected, info.ToFormattedString());
        }

        [Fact]
        public void TwoArgumentConstructorLeavesDetailsUnset()
        {
            var info = new PhysicalMemoryInfo(SixteenGb, EightGb);
            Assert.Null(info.FrequencyMts);
            Assert.Null(info.MemoryType);
        }

        [Fact]
        public void SmbiosPrefersConfiguredSpeedOverRatedSpeed()
        {
            // A DDR4-3600 module running at 2133 (XMP off).
            byte[] table = Table(MemoryDevice(Smbios28Length, Ddr4, speed: 3600, configuredSpeed: 2133));

            Assert.Equal((2133L, "DDR4"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosFallsBackToRatedSpeedBeforeVersion27()
        {
            byte[] table = Table(MemoryDevice(Smbios23Length, Ddr4, speed: 2400));

            Assert.Equal((2400L, "DDR4"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosFallsBackToRatedSpeedWhenConfiguredSpeedIsUnknown()
        {
            byte[] table = Table(MemoryDevice(Smbios28Length, Ddr4, speed: 2400, configuredSpeed: 0));

            Assert.Equal((2400L, "DDR4"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosReadsExtendedSpeedWhenWordIsSaturated()
        {
            byte[] table = Table(MemoryDevice(Smbios33Length, Ddr5, speed: 0xFFFF, configuredSpeed: 0xFFFF, extendedSpeed: 88000, extendedConfiguredSpeed: 70000));

            Assert.Equal((70000L, "DDR5"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosIgnoresSaturatedSpeedWithoutExtendedField()
        {
            byte[] table = Table(MemoryDevice(Smbios28Length, Ddr5, speed: 0xFFFF, configuredSpeed: 0xFFFF));

            Assert.Equal(((long?)null, "DDR5"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosSkipsEmptySlotsAndOtherStructures()
        {
            byte[] table = Table(
                OtherStructure(type: 16, "Board"),
                MemoryDevice(Smbios28Length, UnknownType, speed: 0, configuredSpeed: 0, size: 0, strings: ["DIMM_A1"]),
                MemoryDevice(Smbios28Length, Ddr5, speed: 4800, configuredSpeed: 4800, strings: ["DIMM_A2", "Vendor"]));

            Assert.Equal((4800L, "DDR5"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosReportsSlowestModule()
        {
            byte[] table = Table(
                MemoryDevice(Smbios28Length, Ddr5, speed: 6000, configuredSpeed: 5600),
                MemoryDevice(Smbios28Length, Ddr5, speed: 4800, configuredSpeed: 4800));

            Assert.Equal((4800L, "DDR5"), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosStopsAtEndOfTable()
        {
            byte[] table = Table(
                OtherStructure(type: 127),
                MemoryDevice(Smbios28Length, Ddr5, speed: 4800, configuredSpeed: 4800));

            Assert.Equal(((long?)null, (string?)null), SystemMemory.ParseSmbiosMemoryDevices(table));
        }

        [Fact]
        public void SmbiosToleratesTruncatedTable()
        {
            byte[] full = Table(MemoryDevice(Smbios28Length, Ddr5, speed: 4800, configuredSpeed: 4800));

            Assert.Equal(((long?)null, (string?)null), SystemMemory.ParseSmbiosMemoryDevices(full.AsSpan(0, 10)));
        }

        [Fact]
        public void MacIntelReadsPerSlotItems()
        {
            const string json = """
                {
                  "SPMemoryDataType" : [
                    {
                      "_items" : [
                        { "_name" : "BANK 0/ChannelA-DIMM0", "dimm_size" : "8 GB", "dimm_speed" : "2667 MHz", "dimm_status" : "ok", "dimm_type" : "DDR4" },
                        { "_name" : "BANK 1/ChannelB-DIMM0", "dimm_size" : "empty", "dimm_speed" : "empty", "dimm_status" : "empty", "dimm_type" : "empty" }
                      ],
                      "_name" : "Memory Slots",
                      "global_ecc_state" : "ecc_disabled",
                      "is_memory_upgradeable" : "Yes"
                    }
                  ]
                }
                """;

            Assert.Equal((2667L, "DDR4"), SystemMemory.ParseMacMemoryJson(json));
        }

        [Fact]
        public void MacAppleSiliconReadsTypeWithoutSpeed()
        {
            const string json = """
                {
                  "SPMemoryDataType" : [
                    { "dimm_manufacturer" : "Hynix", "dimm_type" : "LPDDR5", "SPMemoryDataType" : "16 GB" }
                  ]
                }
                """;

            Assert.Equal(((long?)null, "LPDDR5"), SystemMemory.ParseMacMemoryJson(json));
        }

        [Fact]
        public void MacVirtualMachinePlaceholdersAreIgnored()
        {
            // Placeholder values seen on the GitHub Actions macOS arm64 and x64 runners.
            const string arm64 = """{ "SPMemoryDataType" : [ { "dimm_manufacturer" : "Unknown", "dimm_type" : "Unknown", "SPMemoryDataType" : "7 GB" } ] }""";
            const string x64 = """{ "SPMemoryDataType" : [ { "_items" : [ { "_name" : "DIMM0", "dimm_size" : "14 GB", "dimm_speed" : "1600 MHz", "dimm_type" : "RAM" } ] } ] }""";

            Assert.Equal(((long?)null, (string?)null), SystemMemory.ParseMacMemoryJson(arm64));
            Assert.Equal(((long?)null, (string?)null), SystemMemory.ParseMacMemoryJson(x64));
        }

        [Fact]
        public void MacWithoutMemoryDataReturnsNothing()
        {
            Assert.Equal(((long?)null, (string?)null), SystemMemory.ParseMacMemoryJson("""{ "SPMemoryDataType" : [] }"""));
            Assert.Equal(((long?)null, (string?)null), SystemMemory.ParseMacMemoryJson("{}"));
        }

        [Theory]
        [InlineData("Unbuffered-DDR4", "DDR4")]
        [InlineData("Registered-DDR5", "DDR5")]
        [InlineData("Load-Reduced-DDR4-RAM", "DDR4")]
        [InlineData("Low-Power-DDR5-RAM", "LPDDR5")]
        [InlineData("Empty", null)]
        [InlineData("Unknown", null)]
        public void EdacMemoryTypeIsNormalized(string edacType, string? expected)
        {
            Assert.Equal(expected, SystemMemory.NormalizeEdacMemoryType(edacType));
        }

        [Fact]
        public void LinuxPrefersMemAvailableOverMemFree()
        {
            // /proc/meminfo lists MemFree before MemAvailable; the kernel's MemAvailable estimate must win.
            // Arrange
            string[] memInfo =
            [
                "MemTotal:       16777216 kB",
                "MemFree:         262144 kB",
                "MemAvailable:   8388608 kB",
                "Buffers:          123456 kB",
                "Cached:           500000 kB",
            ];

            // Act
            var result = SystemMemory.ParseMemInfo(memInfo);

            // Assert
            result.TotalBytes.Should().Be(SixteenGb);
            result.AvailableBytes.Should().Be(EightGb);
        }

        [Fact]
        public void LinuxAvailableMemoryIsOrderIndependent()
        {
            // Arrange
            string[] memInfo =
            [
                "MemAvailable:   8388608 kB",
                "MemFree:         262144 kB",
            ];

            // Act
            var result = SystemMemory.ParseMemInfo(memInfo);

            // Assert
            result.AvailableBytes.Should().Be(EightGb);
        }

        [Fact]
        public void LinuxFallsBackToMemFreeWithoutMemAvailable()
        {
            // MemAvailable was added in Linux 3.14 and is missing from older kernels.
            // Arrange
            string[] memInfo =
            [
                "MemTotal:       16777216 kB",
                "MemFree:         262144 kB",
            ];

            // Act
            var result = SystemMemory.ParseMemInfo(memInfo);

            // Assert
            result.TotalBytes.Should().Be(SixteenGb);
            result.AvailableBytes.Should().Be(TwoHundredFiftySixMb);
        }

        [Fact]
        public void LinuxWithoutMemoryValuesReturnsNothing()
        {
            // Arrange
            string[] memInfo = ["Buffers: 0 kB", "Cached: 0 kB"];

            // Act
            var result = SystemMemory.ParseMemInfo(memInfo);

            // Assert
            result.TotalBytes.Should().Be(0);
            result.AvailableBytes.Should().BeNull();
        }

        private static byte[] Table(params byte[][] structures)
        {
            var table = new List<byte>();
            foreach (var structure in structures)
                table.AddRange(structure);
            return table.ToArray();
        }

        private static byte[] OtherStructure(byte type, params string[] strings)
        {
            var formatted = new byte[4];
            formatted[0] = type;
            formatted[1] = (byte)formatted.Length;
            return WithStrings(formatted, strings);
        }

        private static byte[] MemoryDevice(byte length, byte memoryType, ushort speed, ushort configuredSpeed = 0, uint extendedSpeed = 0,
            uint extendedConfiguredSpeed = 0, ushort size = 8192, string[]? strings = null)
        {
            var formatted = new byte[length];
            formatted[0] = 17;
            formatted[1] = length;
            BinaryPrimitives.WriteUInt16LittleEndian(formatted.AsSpan(0x0C), size);
            formatted[0x12] = memoryType;
            BinaryPrimitives.WriteUInt16LittleEndian(formatted.AsSpan(0x15), speed);
            if (length >= 0x22)
                BinaryPrimitives.WriteUInt16LittleEndian(formatted.AsSpan(0x20), configuredSpeed);
            if (length >= 0x5C)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(formatted.AsSpan(0x54), extendedSpeed);
                BinaryPrimitives.WriteUInt32LittleEndian(formatted.AsSpan(0x58), extendedConfiguredSpeed);
            }
            return WithStrings(formatted, strings ?? []);
        }

        // The string-set is a sequence of null-terminated strings followed by one more null; with no strings it is two nulls.
        private static byte[] WithStrings(byte[] formatted, string[] strings)
        {
            var bytes = new List<byte>(formatted);
            foreach (var s in strings)
            {
                bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(s));
                bytes.Add(0);
            }
            if (strings.Length == 0)
                bytes.Add(0);
            bytes.Add(0);
            return bytes.ToArray();
        }
        [Fact]
        public void MacAvailableMemoryIncludesReusablePages()
        {
            const string vmStat = """
                Mach Virtual Memory Statistics: (page size of 16384 bytes)
                Pages free:                               100.
                Pages active:                           2000.
                Pages inactive:                          300.
                Pages speculative:                        50.
                Pages throttled:                           0.
                Pages wired down:                       500.
                """;

            Assert.Equal(7_372_800, SystemMemory.ParseMacAvailableMemory(vmStat));
        }

        [Fact]
        public void MacAvailableMemoryReturnsZeroWhenAllAvailablePagesAreZero()
        {
            const string vmStat = """
                Mach Virtual Memory Statistics: (page size of 4096 bytes)
                Pages free:                                 0.
                Pages inactive:                             0.
                Pages speculative:                           0.
                """;

            Assert.Equal(0, SystemMemory.ParseMacAvailableMemory(vmStat));
        }

        [Fact]
        public void MacAvailableMemoryReturnsNullWhenPageDataIsIncomplete()
        {
            Assert.Null(SystemMemory.ParseMacAvailableMemory(
                "Mach Virtual Memory Statistics: (page size of 4096 bytes)"));
        }
    }
}
