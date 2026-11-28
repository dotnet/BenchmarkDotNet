using AwesomeAssertions;
using BenchmarkDotNet.Detectors.Smbios;
using Defaults = BenchmarkDotNet.Tests.Detectors.Smbios.SmbiosTables.MemoryDeviceDefaults;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public partial class SmbiosParserTests
{
    [Fact]
    public void ParseMemoryDevice_ToDisplayString_ShouldReturnExpectedString()
    {
        // Arrange
        var module = SmbiosTables
            .CreateDefaultMemoryDeviceBuilder()
            .BuildMemoryDevice();

        // Act
        var result = module.ToDisplayString();

        // Assert
        result.Should().Be(ExpectedDefaultMemoryDisplayString());
        result.Should().Be(module.ToString());
    }

    [Fact]
    public void ParseMemoryDevice_ToDisplayString_UnknownValues_ShouldOmitUnknown()
    {
        // Arrange
        // Size = 0 means "no module installed", which is skipped by the parser,
        // so the Unknown sentinel (0xFFFF) is used instead.
        var module = SmbiosTables.Type17()
            .WithSize(0xFFFF)
            .WithFormFactor(0)
            .WithDeviceLocator(null)
            .WithBankLocator(null)
            .WithMemoryType(0)
            .WithSpeed(0)
            .WithManufacturer(null)
            .WithSerialNumber(null)
            .WithPartNumber(null)
            .WithRank(0)
            .WithConfiguredSpeed(0)
            .BuildMemoryDevice();

        // Act
        var result = module.ToDisplayString();

        // Assert
        result.Should().BeEmpty(); // Unknown properties are not rendered.
    }

    [Fact]
    public void ParseMemoryDevicesInfo_SingleDdr4Module_ShouldReturnExpectedValues()
    {
        // Arrange
        const ushort TotalWidthBits = 64;
        const ushort DataWidthBits = 64;
        const byte DeviceSet = 1;
        const ushort TypeDetail = 0x2080;
        const ushort VoltageMillivolts = 1200;
        const byte MemoryTechnologyId = 0x03;
        const ushort OperatingModeCapability = 0x0008;
        const ulong UnknownSize = ulong.MaxValue;
        const ulong VolatileSizeBytes = 8UL * 1024 * 1024 * 1024;
        const ulong CacheSizeBytes = 0;

        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder()
            .WithTotalWidth(TotalWidthBits)
            .WithDataWidth(DataWidthBits)
            .WithDeviceSet(DeviceSet)
            .WithTypeDetail(TypeDetail)
            .WithMinVoltage(VoltageMillivolts)
            .WithMaxVoltage(VoltageMillivolts)
            .WithConfiguredVoltage(VoltageMillivolts)
            .WithTechnology(MemoryTechnologyId)
            .WithOperatingMode(OperatingModeCapability)
            .WithNonVolatileSize(UnknownSize)
            .WithVolatileSize(VolatileSizeBytes)
            .WithCacheSize(CacheSizeBytes)
            .WithLogicalSize(UnknownSize)
            .Build();

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.Should().BeEquivalentTo(new SmbiosMemoryDeviceInfo(default, [])
        {
            DeviceLocator = Defaults.DeviceLocator,
            BankLocator = Defaults.BankLocator,
            Manufacturer = Defaults.Manufacturer,
            SerialNumber = Defaults.SerialNumber,
            AssetTag = Defaults.AssetTag,
            PartNumber = Defaults.PartNumber,
            FirmwareVersion = Defaults.FirmwareVersion,
            ModuleManufacturerId = Defaults.ModuleManufacturerId,
            ModuleProductId = Defaults.ModuleProductId,
            ControllerManufacturerId = Defaults.ControllerManufacturerId,
            ControllerProductId = Defaults.ControllerProductId,
            Pmic0ManufacturerId = Defaults.Pmic0ManufacturerId,
            Pmic0RevisionNumber = Defaults.Pmic0Revision,
            RcdManufacturerId = Defaults.RcdManufacturerId,
            RcdRevisionNumber = Defaults.RcdRevision,
            Size = Defaults.Size,
            FormFactorId = Defaults.FormFactor,
            MemoryTypeId = Defaults.MemoryType,
            Speed = Defaults.Speed,
            Attributes = Defaults.Rank,
            ConfiguredMemorySpeed = Defaults.ConfiguredSpeed,
            TotalWidth = TotalWidthBits,
            DataWidth = DataWidthBits,
            DeviceSet = DeviceSet,
            TypeDetail = TypeDetail,
            MinimumVoltageMillivolts = VoltageMillivolts,
            MaximumVoltageMillivolts = VoltageMillivolts,
            ConfiguredVoltageMillivolts = VoltageMillivolts,
            MemoryTechnologyId = MemoryTechnologyId,
            OperatingModeCapability = OperatingModeCapability,
            NonVolatileSize = UnknownSize,
            VolatileSize = VolatileSizeBytes,
            CacheSize = CacheSizeBytes,
            LogicalSize = UnknownSize,
            HasExtendedSize = true,
            HasExtendedSpeed = true,
            HasNonVolatileSize = true,
            HasVolatileSize = true,
            HasCacheSize = true,
            HasLogicalSize = true,
        });
        result.ToDisplayString().Should().Be(ExpectedDefaultMemoryDisplayString());
    }

    [Fact]
    public void ParseMemoryDevice_ToDisplayString_KnownSpdIds_ShouldAppendIds()
    {
        // Arrange
        const byte Ddr5MemoryType = 0x22; // DDR5
        const ushort Ddr5Speed = 4800;
        const byte Ddr5Rank = 1;
        const string Ddr5PartNumber = "M323R2GA3BB0";
        var module = SmbiosTables
            .CreateDefaultMemoryDeviceBuilder()
            .WithMemoryType(Ddr5MemoryType)
            .WithSpeed(Ddr5Speed)
            .WithPartNumber(Ddr5PartNumber)
            .WithRank(Ddr5Rank)
            .WithConfiguredSpeed(Ddr5Speed)
            .BuildMemoryDevice();

        // Act
        var actual = module.ToDisplayString();

        // Assert
        actual.Should().Be(
            $"{Defaults.DeviceLocator} ({Defaults.BankLocator}): " +
            $"{Defaults.Size / 1024} GB {SmbiosDataParser.GetMemoryTypeName(Ddr5MemoryType)} {SmbiosDataParser.GetFormFactorName(Defaults.FormFactor)}, " +
            $"Speed {Ddr5Speed} MT/s (Configured {Ddr5Speed} MT/s), " +
            $"{Defaults.Manufacturer}, Part {Ddr5PartNumber}, " +
            $"Rank {Ddr5Rank}");
    }

    [Fact]
    public void ParseMemoryDevicesInfo_Smbios30Fields_ShouldReturnExpectedValues()
    {
        // Firmware Version and the SPD module/controller IDs were added in SMBIOS 3.0.
        // Arrange
        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder().Build();

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.FirmwareVersion.Should().Be(Defaults.FirmwareVersion);
        result.ModuleManufacturerId.Should().Be(Defaults.ModuleManufacturerId);
        result.ModuleProductId.Should().Be(Defaults.ModuleProductId);
        result.ControllerManufacturerId.Should().Be(Defaults.ControllerManufacturerId);
        result.ControllerProductId.Should().Be(Defaults.ControllerProductId);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_ExtendedSize_ShouldBeUsed()
    {
        // Arrange
        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder().WithSize(0x7FFF).WithExtendedSizeMb(32768).Build();

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.ResolvedSizeBytes.Should().Be(32768L * 1024 * 1024);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_SizeInKilobytes_ShouldDecodeBytes()
    {
        // Arrange
        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder().WithSize(0x8000 | 256).Build();

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.ResolvedSizeBytes.Should().Be(256L * 1024);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_EmptySlot_ShouldBeSkipped()
    {
        // Arrange
        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder().WithSize(0).Build();

        // Act
        var results = SmbiosDataParser.ParseMemoryDevicesInfo(table);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void ParseMemoryDevicesInfo_UnknownSize_ShouldBeIncludedWithUnknownBytes()
    {
        // Arrange
        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder().WithSize(0xFFFF).Build();

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.ResolvedSizeBytes.Should().Be(0);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_ExtendedSpeed_ShouldBeUsed()
    {
        {
            // Extended Speed takes over when the 16-bit Speed is Unknown.
            // Arrange
            var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder()
                 .WithSpeed(0xFFFF)
                 .WithExtendedSpeedMTs(4800)
                 .WithConfiguredSpeed(0xFFFF)
                 .Build();

            // Act
            var result = ParseSingleMemoryDevice(table);

            // Assert
            result.ResolvedSpeedMTs.Should().Be(4800);
            result.ResolvedConfiguredSpeedMTs.Should().Be(0);
        }
        {
            // A value beyond the 16-bit range is ignored.
            // Arrange
            var capped = SmbiosTables.CreateDefaultMemoryDeviceBuilder()
                .WithExtendedSpeedMTs(9999)
                .Build();

            // Act
            var cappedDevice = ParseSingleMemoryDevice(capped);

            // Assert
            cappedDevice.ResolvedSpeedMTs.Should().Be(Defaults.Speed);
        }
    }

    [Fact]
    public void ParseMemoryDevicesInfo_ExtendedConfiguredSpeed_ShouldBeUsed()
    {
        {
            // Extended Configured Memory Speed was added in SMBIOS 3.3.
            // Arrange
            var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder()
                .WithConfiguredSpeed(0xFFFF)
                .WithExtendedConfiguredSpeedMTs(0x80000000u | 4800) // Bit 31 is reserved.
                .Build();

            // Act
            var result = ParseSingleMemoryDevice(table);

            // Assert
            result.HasExtendedSpeed.Should().BeTrue();
            result.ExtendedConfiguredSpeedMTs.Should().Be(0x80000000u | 4800);
            result.ResolvedConfiguredSpeedMTs.Should().Be(4800);
        }
        {
            // A value beyond the 16-bit range is ignored.
            // Arrange
            byte[] capped = SmbiosTables.CreateDefaultMemoryDeviceBuilder()
                .WithExtendedConfiguredSpeedMTs(9999)
                .Build();

            // Act
            SmbiosMemoryDeviceInfo cappedDevice = ParseSingleMemoryDevice(capped);

            // Assert
            cappedDevice.ResolvedConfiguredSpeedMTs.Should().Be(Defaults.ConfiguredSpeed);
        }
    }

    [Fact]
    public void ParseMemoryDevicesInfo_PmicAndRcdFields_ShouldReturnExpectedValues()
    {
        // Arrange
        var table = SmbiosTables.CreateDefaultMemoryDeviceBuilder().Build();

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.Pmic0ManufacturerId.Should().Be(Defaults.Pmic0ManufacturerId);
        result.Pmic0RevisionNumber.Should().Be(Defaults.Pmic0Revision);
        result.RcdManufacturerId.Should().Be(Defaults.RcdManufacturerId);
        result.RcdRevisionNumber.Should().Be(Defaults.RcdRevision);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_Pre37ShortStructure_ShouldReportRevisionFieldsAsUnknown()
    {
        // Arrange
        var full = SmbiosTables.CreateDefaultMemoryDeviceBuilder().Build();

        // Truncate the formatted area to the pre SMBIOS 3.7 length (0x5C)
        var table = SmbiosTables.TruncateFormattedArea(full, 0x5C);

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.Pmic0ManufacturerId.Should().Be(0);
        result.Pmic0RevisionNumber.Should().Be(0xFF00);
        result.RcdManufacturerId.Should().Be(0);
        result.RcdRevisionNumber.Should().Be(0xFF00);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_Pre30ShortStructure_ShouldReportSmbios30FieldsAsAbsent()
    {
        // Arrange
        var full = SmbiosTables.CreateDefaultMemoryDeviceBuilder().Build();

        // Truncate the formatted area to the SMBIOS 2.8 length (0x28):
        // Firmware Version and the SPD module/controller IDs are absent.
        var table = SmbiosTables.TruncateFormattedArea(full, 0x28);

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.FirmwareVersion.Should().BeNull();
        result.ModuleManufacturerId.Should().Be(0);
        result.ModuleProductId.Should().Be(0);
        result.ControllerManufacturerId.Should().Be(0);
        result.ControllerProductId.Should().Be(0);
    }

    [Fact]
    public void ParseMemoryDevicesInfo_TruncatedStructure_ShouldReportExtendedFieldsAsAbsent()
    {
        // Arrange
        var full = SmbiosTables.CreateDefaultMemoryDeviceBuilder().Build();

        // Truncate the formatted area to the 2.6 length (0x1C): Extended Size, the QWORD sizes
        // and the extended speeds are absent.
        var table = SmbiosTables.TruncateFormattedArea(full, 0x1C);

        // Act
        var result = ParseSingleMemoryDevice(table);

        // Assert
        result.HasExtendedSize.Should().BeFalse();
        result.HasExtendedSpeed.Should().BeFalse();
        result.HasNonVolatileSize.Should().BeFalse();
        result.HasVolatileSize.Should().BeFalse();
        result.HasCacheSize.Should().BeFalse();
        result.HasLogicalSize.Should().BeFalse();
        result.ExtendedSize.Should().Be(0);
        result.ExtendedSpeedMTs.Should().Be(0);
        result.NonVolatileSizeBytes.Should().BeNull();
        result.ResolvedSizeBytes.Should().Be(Defaults.Size * 1024L * 1024);
        result.ResolvedSpeedMTs.Should().Be(Defaults.Speed);
        // Configured Memory Speed (0x20) is also beyond the truncated area.
        result.ConfiguredMemorySpeed.Should().Be(0);
        result.ResolvedConfiguredSpeedMTs.Should().Be(0);
    }


    [Theory]
    [InlineData((ushort) 0, "Unknown")]
    [InlineData((ushort) 1200, "1.2 V")]
    [InlineData((ushort) 1350, "1.35 V")]
    [InlineData((ushort) 1100, "1.1 V")]
    public void ParseMemoryDevice_ToDisplayVoltage_WithMillivolts_ShouldFormatVoltage(ushort millivolts, string expected)
    {
        // Act
        var result = SmbiosDisplayExtensions.ToDisplayVoltage(millivolts);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData((byte) 0x00, "Reserved")]
    [InlineData((byte) 0x02, "Unknown")]
    [InlineData((byte) 0x18, "DDR3")]
    [InlineData((byte) 0x1A, "DDR4")]
    [InlineData((byte) 0x22, "DDR5")]
    [InlineData((byte) 0x23, "LPDDR5")]
    [InlineData((byte) 0x24, "HBM3")]
    [InlineData((byte) 0x25, "MRDIMM")]
    [InlineData((byte) 0x26, "LPDDR6")]
    [InlineData((byte) 0x27, "Unknown (0x27)")]
    public void ParseMemoryDevice_GetMemoryTypeName_KnownIds_ShouldMatchSpecification(byte id, string expected)
    {
        // Act
        var result = SmbiosDataParser.GetMemoryTypeName(id);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData((byte) 0x09, "DIMM")]
    [InlineData((byte) 0x0D, "SODIMM")]
    [InlineData((byte) 0x10, "Die")]
    [InlineData((byte) 0x11, "CAMM")]
    [InlineData((byte) 0x12, "CUDIMM")]
    [InlineData((byte) 0x13, "CSODIMM")]
    [InlineData((byte) 0x00, "Reserved")]
    [InlineData((byte) 0x14, "Unknown (0x14)")]
    public void ParseMemoryDevice_GetFormFactorName_KnownIds_ShouldMatchSpecification(byte id, string expected)
    {
        // Act
        var actual = SmbiosDataParser.GetFormFactorName(id);

        // Assert
        actual.Should().Be(expected);
    }

    private static string ExpectedDefaultMemoryDisplayString()
        => $"{Defaults.DeviceLocator} ({Defaults.BankLocator}): " +
           $"{Defaults.Size / 1024} GB {SmbiosDataParser.GetMemoryTypeName(Defaults.MemoryType)} {SmbiosDataParser.GetFormFactorName(Defaults.FormFactor)}, " +
           $"Speed {Defaults.Speed} MT/s (Configured {Defaults.ConfiguredSpeed} MT/s), " +
           $"{Defaults.Manufacturer}, Part {Defaults.PartNumber}, " +
           $"Rank {Defaults.Rank}";
}
