﻿using BenchmarkDotNet.Detectors.Cpu.Windows;
using Perfolizer.Models;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Detectors.Cpu;

public class PowershellWmiCpuInfoParserTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;


    [Fact]
    public void EmptyTest()
    {
        CpuInfo? actual = PowershellWmiCpuInfoParser.Parse(string.Empty);
        CpuInfo expected = new CpuInfo();
        Output.AssertEqual(expected, actual);
    }


    [Fact]
    public void MalformedTest()
    {
        CpuInfo? actual = PowershellWmiCpuInfoParser
            .Parse("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
        CpuInfo expected = new CpuInfo();
        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealTwoProcessorEightCoresTest()
    {
        const string cpuInfo =
            """
            MaxClockSpeed:2400
            Name:Intel(R) Xeon(R) CPU E5-2630 v3
            NumberOfCores:8
            NumberOfLogicalProcessors:16
                            
                          
            MaxClockSpeed:2400
            Name:Intel(R) Xeon(R) CPU E5-2630 v3
            NumberOfCores:8
            NumberOfLogicalProcessors:16
            
            """;
        CpuInfo? actual = PowershellWmiCpuInfoParser.Parse(cpuInfo);

        CpuInfo expected = new CpuInfo
        {
            ProcessorName = "Intel(R) Xeon(R) CPU E5-2630 v3",
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = 16,
            LogicalCoreCount = 32,
            NominalFrequencyHz = 2_400_000_000,
            MaxFrequencyHz = 2_400_000_000,
        };

        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealTwoProcessorEightCoresWithWmicBugTest()
    {
        const string cpuInfo =
            "\r\r\n" +
            "\r\r\n" +
            "MaxClockSpeed:3111\r\r\n" +
            "Name:Intel(R) Xeon(R) CPU E5-2687W 0\r\r\n" +
            "NumberOfCores:8\r\r\n" +
            "NumberOfLogicalProcessors:16\r\r\n" +
            "\r\r\n" +
            "\r\r\n" +
            "MaxClockSpeed:3111\r\r\n" +
            "Name:Intel(R) Xeon(R) CPU E5-2687W 0\r\r\n" +
            "NumberOfCores:8\r\r\n" +
            "NumberOfLogicalProcessors:16\r\r\n" +
            "\r\r\n" +
            "\r\r\n" +
            "\r\r\n";

        CpuInfo? actual = PowershellWmiCpuInfoParser.Parse(cpuInfo);

        CpuInfo expected = new CpuInfo
        {
            ProcessorName = "Intel(R) Xeon(R) CPU E5-2687W 0",
            PhysicalProcessorCount = 2,
            PhysicalCoreCount = 16,
            LogicalCoreCount = 32,
            NominalFrequencyHz = 3_111_000_000,
            MaxFrequencyHz = 3_111_000_000,
        };

        Output.AssertEqual(expected, actual);
    }

    [Fact]
    public void RealOneProcessorFourCoresTest()
    {
        const string cpuInfo = """

            MaxClockSpeed:2500
            Name:Intel(R) Core(TM) i7-4710MQ
            NumberOfCores:4
            NumberOfLogicalProcessors:8
            """;

        CpuInfo? actual = PowershellWmiCpuInfoParser.Parse(cpuInfo);
        CpuInfo expected = new CpuInfo
        {
            ProcessorName = "Intel(R) Core(TM) i7-4710MQ",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 4,
            LogicalCoreCount = 8,
            NominalFrequencyHz = 2_500_000_000,
            MaxFrequencyHz = 2_500_000_000,
        };

        Output.AssertEqual(expected, actual);
    }
}