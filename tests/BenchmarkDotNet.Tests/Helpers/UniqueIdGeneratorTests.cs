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
            "1ed4411f-c92d-8f60-aadd-fbd8cc0c3270",
            "1b3f5c2e-38be-8d60-8a0f-220aa548549b",
            "16d91eb5-6b8d-8399-8843-bee86dd80bce",
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
            "171ce586-f86d-8b4f-ad25-617e3f02c6cf",
            "1daf0e59-c7d1-8d97-84f2-7bdf6b45ff73",
            "164dce04-85a5-8215-a9b2-2c1a742fa2ea",

            "15ef264f-fcde-89bd-9d7e-42e7307b2d29",
            "1b3fce88-39fe-87ae-96fe-d1024fd1cc09",
            "1c8797a3-aded-8850-b983-32f592e25bf4",

            "164ffb49-b0d1-8099-8e55-e82bc5f48d71",
            "1c3e6d12-1e8b-85bc-a3a1-8e84bfa378ba",
            "1a4c7c21-9459-8ad6-b101-23386b4b0996",
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
