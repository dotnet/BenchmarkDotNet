using AwesomeAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.NetCoreApp;

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
        var uids = benchmarkCases.Select(x => x.GetUniqueId()).ToArray();

        // Assert
        uids.Should().BeEquivalentTo(
        [
            "14662b5a-18fb-819c-a791-7eb540aa1257",
            "1d4297b4-729e-8196-b884-4085499599d4",
            "10657006-42ba-8c9b-af8b-ef62dfdd91a6",
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
                Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80),
            ]);

        var benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks), config)!;
        var benchmarkCases = benchmarkRunInfo.BenchmarksCases;

        // Act
        var uids = benchmarkCases.Select(x => x.GetUniqueId()).ToArray();

        // Assert
        uids.Should().BeEquivalentTo(
        [
            "140e2750-1d92-8a60-ae3a-8edba6a46f27",
            "1ac7693b-dd06-890d-88cb-04758d231ace",
            "1e1acf16-fda2-88bd-a020-d69af851df96",

            "18ba3702-d8a3-8666-9b61-48e7a90e784a",
            "15fa977d-8202-81f3-abf4-c6d0c9d5b18c",
            "137fba29-6929-8213-9e65-3b4bee0bf72d",
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
