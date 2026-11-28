using AwesomeAssertions;
using BenchmarkDotNet.Detectors.Smbios;
using System.Text.Json;
using Defaults = BenchmarkDotNet.Tests.Detectors.Smbios.SmbiosTables.MemoryDeviceDefaults;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public partial class SmbiosParserTests : SmbiosTestBase
{
    private readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    private static readonly byte[] EndOfTableBytes =
    [
        (byte)SmbiosType.EndOfTable, // 0x00: Type
        4,                           // 0x01: Length
        0, 0,                        // 0x02-0x03: Handle
        0, 0,                        // Empty string-set terminator
    ];

    public SmbiosParserTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void ParseSmbiosData_And_PrintResults()
    {
        if (!IsSmbiosAvailable() || !SmbiosDataAccessor.TryGetSmbiosData(out var smbiosVersion, out var bytes))
            return; // TODO: Use Assert.Skip after migrated to xUnit.v3

        // Print SMBIOS version.
        Output.WriteLine($"SMBIOS Version: {smbiosVersion}");
        Output.WriteLine($"Data Length: {bytes.Length}");

        // Print CPU information
        {
            // Act
            var processors = SmbiosDataParser.ParseProcessorInfoTable(bytes);

            // Assert
            processors.Should().NotBeEmpty();

            // Output collected processor information.
            foreach (var processor in processors)
            {
                Output.WriteLine(processor.ToDisplayString());
            }
            Output.WriteLine(JsonSerializer.Serialize(processors, DefaultSerializerOptions));
        }

        // Print Memory information
        {
            // Act
            var memoryDevices = SmbiosDataParser.ParseMemoryDevicesInfo(bytes);

            // Assert
            memoryDevices.Should().NotBeEmpty();

            // Output collected memory information.
            foreach (var module in memoryDevices)
            {
                Output.WriteLine(module.ToDisplayString());
            }
            Output.WriteLine(JsonSerializer.Serialize(memoryDevices, DefaultSerializerOptions));
        }
    }

    [Fact]
    public void ParseProcessorInfoTableAndMemoryDevicesInfo_MixedTable_ShouldParseEachType()
    {
        // Arrange
        var cpuTable = SmbiosTables.CreateDefaultProcessorBuilder().Build();
        var memoryTable = SmbiosTables.CreateDefaultMemoryDeviceBuilder().Build();
        var table = Enumerable.Concat(cpuTable, memoryTable)
                                 .Concat(EndOfTableBytes)
                                 .ToArray();

        // Act/Assert
        ParseSingleProcessor(table).Should().NotBeNull();
        ParseSingleMemoryDevice(table).ResolvedSizeBytes.Should().Be(Defaults.Size * 1024L * 1024);
    }

    private static SmbiosProcessorInfo ParseSingleProcessor(byte[] table)
    {
        var results = SmbiosDataParser.ParseProcessorInfoTable(table);
        results.Should().HaveCount(1);
        return results[0];
    }

    private static SmbiosMemoryDeviceInfo ParseSingleMemoryDevice(byte[] table)
    {
        var results = SmbiosDataParser.ParseMemoryDevicesInfo(table);
        results.Should().HaveCount(1);
        return results[0];
    }
}
