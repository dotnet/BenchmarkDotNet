using AwesomeAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Tests.Helpers;

public class UniqueIdGeneratorTests
{
    [Fact]
    public void VerifyGeneratedUids()
    {
        // Arrange
        var config = ManualConfig.CreateEmpty();
        var benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks), config)!;
        var benchmarkCases = benchmarkRunInfo.BenchmarksCases;

        // Act
        var uids = benchmarkCases.Select(UniqueIdGenerator.FromBenchmarkCase).ToArray();

        // Assert
        uids.Should().BeEquivalentTo(
        [
            "1b61bc29-076c-8aec-8328-aea9075252d1",
            "1f2340bc-6788-8475-89a2-2db2150530dd",
            "17acc5a4-e46f-81d9-adc6-3b173360451d",
        ]);

        uids.Should().AllSatisfy(Validate);
    }

    [Fact]
    public void VerifyGeneratedUIds_WithCustomJobId()
    {
        // Arrange
        var config = ManualConfig.CreateEmpty()
            .AddJob(
            [
                Job.Dry.WithId("TestJob"),
                Job.Dry.WithRuntime(CoreRuntime.Core80),
                Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80),
            ]);

        var benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks), config)!;
        var benchmarkCases = benchmarkRunInfo.BenchmarksCases;

        // Act
        var uids = benchmarkCases.Select(UniqueIdGenerator.FromBenchmarkCase).ToArray();

        // Assert
        uids.Should().BeEquivalentTo(
        [
            "155d621e-0e90-8856-85e0-2417c2be972b",
            "109d9783-e792-8f51-9c64-2d2f734081da",
            "1fb185df-b999-8325-b1fe-7cd4a89f5d80",

            "17cd4765-f9ad-8dc3-9047-0c955e68a0e5",
            "1a15c08b-0b56-8df1-a643-0b8bf487d5d2",
            "163eee05-81d5-8e7c-906d-244be9d2625f",

            "16433a2e-983a-8deb-bbd2-9a9b8b653bcd",
            "16a7f2d3-c14b-84a9-baaa-ca1269efdfe0",
            "172e05ae-eae0-8282-8455-1135f67df34f",
        ]);

        uids.Should().AllSatisfy(Validate);
    }

    private void Validate(string guidText)
    {
        var guid = Guid.Parse(guidText);

        // Verify embedded hash version in generated uid.
        const byte expectedUidHashVersion = 1;
        GetEmbeddedHashVersion(guid).Should().Be(expectedUidHashVersion);

#if NET9_0_OR_GREATER
        guid.Version.Should().Be(8);

        // According to the specification only the first 2 bits are used for UUID v8.
        int variant = guid.Variant >> 2;
        variant.Should().Be(2);
#endif
    }

    private static byte GetEmbeddedHashVersion(Guid guid)
    {
        var b = guid.ToByteArray();
        return (byte)(b[3] >> 4);
    }

    public class Benchmarks
    {
        [Benchmark]
        public void Benchmark01()
        {
        }

        [Benchmark]
        public void Benchmark02()
        {
        }

        [Benchmark]
        public void Benchmark03()
        {
        }
    }
}
