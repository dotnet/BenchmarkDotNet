using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Detectors.Smbios;
using Perfolizer.Models;
using Defaults = BenchmarkDotNet.Tests.Detectors.Smbios.SmbiosTables.ProcessorDefaults;
using Status = BenchmarkDotNet.Tests.Detectors.Smbios.SmbiosTables.Status;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public partial class SmbiosParserTests
{
    [Fact]
    public void GetProcessorInfo_OnWindows_ShouldReturnPopulatedSockets()
    {
        if (!OsDetector.IsWindows7OrLater())
            return; // TODO: Repace to Assert.Skip after migrated to xUnit.v3

        // Act
        var processorsInfo = SmbiosDataAccessor.GetProcessorInfo();

        // Assert
        processorsInfo.Should().NotBeNullOrEmpty();
        foreach (SmbiosProcessorInfo cpu in processorsInfo)
        {
            Output.WriteLine(cpu.ToDisplayString());
        }
    }

    [Fact]
    public void ParseProcessor_ToDisplayString_FullEntry_ShouldReturnExpectedString()
    {
        // Arrange
        SmbiosProcessorInfo cpu = SmbiosTables.CreateDefaultProcessorBuilder()
            .BuildProcessorInfo();

        // Act
        string actual = cpu.ToDisplayString();

        // Assert
        actual.Should().Be(ExpectedDefaultProcessorDisplayString());
        cpu.ToString().Should().Be(actual);
    }

    [Fact]
    public void ParseProcessor_ToDisplayString_PaddedVersion_ShouldTrimPadding()
    {
        // Real firmware often pads the string with trailing spaces.
        // Arrange
        SmbiosProcessorInfo cpu = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithVersion(Defaults.Version + new string(' ', 14))
            .BuildProcessorInfo();

        // Act
        string actual = cpu.ToDisplayString();

        // Assert
        actual.Should().StartWith($"{Defaults.Version}, ");
    }

    [Fact]
    public void ParseProcessor_ToDisplayString_UnknownValues_ShouldOmitUnknown()
    {
        // Arrange
        SmbiosProcessorInfo cpu = SmbiosTables.Type4()
            .WithSocket(null)
            .WithManufacturer(null)
            .WithVersion(null)
            .WithProcessorId([0, 0, 0, 0, 0, 0, 0, 0]) // An all-zero ID is rendered as Unknown.
            .WithExternalClockMHz(0)
            .WithMaxSpeedMHz(0)
            .WithCurrentSpeedMHz(0)
            .WithStatus(Status.Default)
            .WithCoreCount(0)
            .WithCoreEnabled(0)
            .WithThreadCount(0)
            .BuildProcessorInfo();

        // Act
        string actual = cpu.ToDisplayString();

        // Assert
        actual.Should().BeEmpty(); // Unknown properties are not rendered.
    }

    [Fact]
    public void ParseProcessorInfoTable_SingleProcessor_ShouldReturnExpectedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithProcessorType(3)
            .WithProcessorFamily(0x2B)
            .WithVoltage(0x88)
            .WithProcessorCharacteristics(0x0201)
            .WithProcessorFamily2(0x2B)
            .Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.Should().BeEquivalentTo(new SmbiosProcessorInfo(default, [])
        {
            SocketDesignation = Defaults.Socket,
            ProcessorType = 3,
            ProcessorFamily = 0x2B,
            ProcessorManufacturer = Defaults.Manufacturer,
            ProcessorId = Defaults.ProcessorIdString,
            ProcessorVersion = Defaults.Version,
            Voltage = 0x88,
            ExternalClockMHz = Defaults.ExternalClockMHz,
            MaxSpeedMHz = Defaults.MaxSpeedMHz,
            CurrentSpeedMHz = Defaults.CurrentSpeedMHz,
            Status = Defaults.Status,
            CoreCount = Defaults.CoreCount,
            CoreEnabled = Defaults.CoreEnabled,
            ThreadCount = Defaults.ThreadCount,
            ThreadEnabled = Defaults.ThreadEnabled,
            ProcessorCharacteristics = 0x0201,
            ProcessorFamily2 = 0x2B,
            SocketType = Defaults.SocketType,
            HasCoreCount2 = true,
            HasCoreEnabled2 = true,
            HasThreadCount2 = true,
        });
    }

    [Fact]
    public void ParseProcessorInfoTable_ExtendedCounts_ShouldUseExtendedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithSocket("LGA1700")
            .WithManufacturer("Intel")
            .WithVersion("Core i9")
            .WithMaxSpeedMHz(5000)
            .WithCurrentSpeedMHz(4800)
            .WithCoreCount(0xFF)
            .WithCoreEnabled(0xFF)
            .WithThreadCount(0xFF)
            .WithCoreCount2(32)
            .WithCoreEnabled2(28)
            .WithThreadCount2(56)
            .Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.HasCoreCount2.Should().BeTrue();
        cpu.HasCoreEnabled2.Should().BeTrue();
        cpu.HasThreadCount2.Should().BeTrue();
        cpu.CoreCount.Should().Be(0xFF); // The raw field selects the extended count.
        cpu.CoreCount2.Should().Be(32);
        cpu.ResolvedCoreCount.Should().Be(32);
        cpu.CoreEnabled.Should().Be(0xFF);
        cpu.CoreEnabled2.Should().Be(28);
        cpu.ResolvedCoreEnabledCount.Should().Be(28);
        cpu.ThreadCount.Should().Be(0xFF);
        cpu.ThreadCount2.Should().Be(56);
        cpu.ResolvedThreadCount.Should().Be(56);
    }

    [Fact]
    public void ParseProcessorInfoTable_ThreadEnabledAndSocketType_ShouldReturnExpectedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithThreadCount(12)
            .WithThreadEnabled(8)
            .Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.ThreadEnabled.Should().Be(8);
        cpu.ThreadEnabledCount.Should().Be(8);
        cpu.SocketType.Should().Be(Defaults.SocketType);
    }

    [Fact]
    public void ParseProcessorInfoTable_ThreadEnabledReserved_ShouldBeUnknown()
    {
        // Arrange
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithThreadCount(12)
            .WithThreadEnabled(0xFFFF)
            .Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.ThreadEnabled.Should().Be(0xFFFF); // FFFFh is reserved for Unknown.
        cpu.ThreadEnabledCount.Should().Be(0);
    }

    [Fact]
    public void ParseProcessorInfoTable_AllZeroProcessorId_ShouldBeUnknown()
    {
        // Arrange
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithProcessorId([0, 0, 0, 0, 0, 0, 0, 0])
            .WithThreadCount(12)
            .Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.ProcessorId.Should().BeNull(); // An all-zero ID is treated as Unknown.
    }

    [Fact]
    public void ParseProcessorInfoTable_Pre36ShortStructure_ShouldReportNewFieldsAsUnknown()
    {
        // Arrange
        byte[] full = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithThreadCount(12)
            .WithThreadEnabled(8)
            .Build();

        // Truncate the formatted area to the pre-3.6 length (0x30): the 3.6+ fields are absent.
        byte[] table = SmbiosTables.TruncateFormattedArea(full, 0x30);

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.ThreadEnabled.Should().Be(0); // The 3.6+ field is absent, so the raw value is Unknown (0).
        cpu.ThreadEnabledCount.Should().Be(0);
        cpu.SocketType.Should().BeNull();
    }

    [Fact]
    public void ParseProcessorInfoTable_Pre30ShortStructure_ShouldReportSmbios30FieldsAsUnknown()
    {
        // Arrange
        byte[] full = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithThreadCount(12)
            .WithProcessorFamily2(0x2B)
            .WithCoreCount2(32)
            .WithCoreEnabled2(28)
            .WithThreadCount2(64)
            .Build();

        // Truncate the formatted area to the pre-3.0 length (0x28): the 3.0 fields are absent.
        byte[] table = SmbiosTables.TruncateFormattedArea(full, 0x28);

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.HasCoreCount2.Should().BeFalse();
        cpu.HasCoreEnabled2.Should().BeFalse();
        cpu.HasThreadCount2.Should().BeFalse();
        cpu.ProcessorFamily2.Should().Be(0);
        cpu.CoreCount2.Should().Be(0);
        cpu.CoreEnabled2.Should().Be(0);
        cpu.ThreadCount2.Should().Be(0);

        // Verify count properties are available.
        cpu.ResolvedCoreCount.Should().Be(Defaults.CoreCount);
        cpu.ResolvedCoreEnabledCount.Should().Be(Defaults.CoreEnabled);
        cpu.ResolvedThreadCount.Should().Be(12);
    }

    [Fact]
    public void ParseProcessorInfoTable_UnpopulatedSocket_ShouldBeSkipped()
    {
        // Arrange
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithStatus(Status.UnPopulated)
            .Build();

        // Act
        var results = SmbiosDataParser.ParseProcessorInfoTable(table);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void ParseProcessorInfoTable_PopulatedNotEnabledSocket_ShouldBeIncluded()
    {
        // Arrange
        // Bit 6 (populated) is set, so the socket is parsed even though the CPU status is 0h (Unknown).
        byte[] table = SmbiosTables.CreateDefaultProcessorBuilder()
            .WithStatus(Status.PopulatedNotEnabled)
            .Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.Status.Should().Be(0x40);
        cpu.IsEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData((byte) 0x41, true)]  // Populated & CPU status 1h (Enabled)
    [InlineData((byte) 0x40, false)] // Populated & CPU status 0h (Unknown)
    [InlineData((byte) 0x42, false)] // Populated & CPU status 2h (Disabled By User)
    [InlineData((byte) 0x43, false)] // Populated & CPU status 3h (Disabled By BIOS)
    [InlineData((byte) 0x44, false)] // Populated & CPU status 4h (Idle)
    [InlineData((byte) 0x47, false)] // Populated & CPU status 7h (Other)
    public void ParseProcessorInfoTable_CpuStatus_ShouldResolveIsEnabled(byte status, bool expected)
    {
        // Arrange
        byte[] table = SmbiosTables.Type4().WithStatus(status).Build();

        // Act
        var cpu = ParseSingleProcessor(table);

        // Assert
        cpu.IsEnabled.Should().Be(expected);
    }

    [Fact]
    public void FromProcessorInfo_MultipleSockets_ShouldAggregateSockets()
    {
        // Arrange
        var cpus = new List<SmbiosProcessorInfo>
        {
            CreateProcessorInfo("CPU0"),
            CreateProcessorInfo("CPU1"),
        };

        // Act
        var cpuInfo = SmbiosCpuDetector.FromProcessorInfo(cpus);

        // Assert
        cpuInfo.Should().BeEquivalentTo(new CpuInfo
        {
            ProcessorName = Defaults.Version,
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = Defaults.CoreCount * 2,
            LogicalCoreCount = Defaults.ThreadCount * 2,
            MaxFrequencyHz = Defaults.MaxSpeedMHz * 1000L * 1000,
            NominalFrequencyHz = Defaults.CurrentSpeedMHz * 1000L * 1000,
        });
    }

    [Fact]
    public void FromProcessorInfo_PopulatedNotEnabledSocket_ShouldBeExcluded()
    {
        // Arrange
        var cpus = new List<SmbiosProcessorInfo>
        {
            CreateProcessorInfo("CPU0"),
            CreateProcessorInfo("CPU1", version: "Disabled CPU", status: Status.PopulatedNotEnabled, threadCount: 12),
        };

        // Act
        var cpuInfo = SmbiosCpuDetector.FromProcessorInfo(cpus);

        // Assert
        cpuInfo.Should().BeEquivalentTo(new CpuInfo
        {
            ProcessorName = Defaults.Version, // The disabled socket does not contribute its version.
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = Defaults.CoreCount,
            LogicalCoreCount = Defaults.ThreadCount,
            MaxFrequencyHz = Defaults.MaxSpeedMHz * 1000L * 1000,
            NominalFrequencyHz = Defaults.CurrentSpeedMHz * 1000L * 1000,
        });
    }

    [Fact]
    public void FromProcessorInfo_NoEnabledSocket_ShouldReturnNull()
    {
        // Arrange
        var cpus = new List<SmbiosProcessorInfo>
        {
            CreateProcessorInfo("CPU0", status: Status.PopulatedNotEnabled),
        };

        // Act
        var cpuInfo = SmbiosCpuDetector.FromProcessorInfo(cpus);

        // Assert
        cpuInfo.Should().BeNull();
    }

    [Fact]
    public void FromProcessorInfo_Empty_ShouldReturnNull()
    {
        // Act
        var cpuInfo = SmbiosCpuDetector.FromProcessorInfo([]);

        // Assert
        cpuInfo.Should().BeNull();
    }

    [Fact]
    public void FromProcessorInfo_UnknownThreadCount_ShouldFallBackToThreadEnabled()
    {
        // Arrange
        var cpus = new List<SmbiosProcessorInfo>
        {
            CreateProcessorInfo("CPU0", threadCount: 0, threadEnabled: 8),
        };

        // Act
        var cpuInfo = SmbiosCpuDetector.FromProcessorInfo(cpus);

        // Assert
        cpuInfo.Should().BeEquivalentTo(new CpuInfo
        {
            ProcessorName = Defaults.Version,
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = Defaults.CoreCount,
            LogicalCoreCount = 8,
            MaxFrequencyHz = Defaults.MaxSpeedMHz * 1000L * 1000,
            NominalFrequencyHz = Defaults.CurrentSpeedMHz * 1000L * 1000,
        });
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0, 0, 0 })]
    [InlineData(new byte[] { 4, 2, 0, 0, 0, 0 })]
    public void ParseProcessorInfoTable_InvalidData_ShouldNotThrow(byte[] tableData)
    {
        // Arrange
        IReadOnlyList<SmbiosProcessorInfo> results = default!;
        Action act = () => results = SmbiosDataParser.ParseProcessorInfoTable(tableData);

        // Act
        act.Should().NotThrow();

        // Assert
        results.Should().BeEmpty();
    }

    private static SmbiosProcessorInfo CreateProcessorInfo(
        string socket,
        string? version = null,
        Status? status = null,
        byte? threadCount = null,
        ushort? threadEnabled = null)
    {
        var builder = SmbiosTables.CreateDefaultProcessorBuilder()
                                  .WithSocket(socket);

        if (version is not null)
            builder.WithVersion(version);

        if (status is not null)
            builder.WithStatus(status.Value);

        if (threadCount is not null)
            builder.WithThreadCount(threadCount.Value);

        if (threadEnabled is not null)
            builder.WithThreadEnabled(threadEnabled.Value);

        return builder.BuildProcessorInfo();
    }

    private static string ExpectedDefaultProcessorDisplayString()
        => $"{Defaults.Version}, " +
           $"External {Defaults.ExternalClockMHz}MHz, " +
           $"Max {Defaults.MaxSpeedMHz}MHz, Current {Defaults.CurrentSpeedMHz}MHz, " +
           $"{Defaults.CoreCount} cores ({Defaults.CoreEnabled} enabled)";
}
