using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Validators;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators;

public class RuntimeValidatorTests
{
    [Fact]
    public async Task SameRuntime_Should_Success()
    {
        // Arrange
        var config = new TestConfig1().CreateImmutableConfig();
        var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(DummyBenchmark), config);
        var parameters = new ValidationParameters(runInfo.BenchmarksCases, config);

        // Act
        var errors = await RuntimeValidator.DontFailOnError.ValidateAsync(parameters).Select(e => e.Message).ToArrayAsync();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task NullRuntimeMixed_Should_Failed()
    {
        // Arrange
        var config = new TestConfig2().CreateImmutableConfig();
        var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(DummyBenchmark), config);
        var parameters = new ValidationParameters(runInfo.BenchmarksCases, config);

        // Act
        var errors = await RuntimeValidator.DontFailOnError.ValidateAsync(parameters).Select(e => e.Message).ToArrayAsync();

        // Assert
        {
            var expectedMessage = "Job(Dry) doesn't have a Runtime characteristic. It's recommended to specify runtime by using WithRuntime explicitly.";
            Assert.Contains(expectedMessage, errors);
        }
        {
            var expectedMessage = "Job(Toolchain=.NET 11.0) doesn't have a Runtime characteristic. It's recommended to specify runtime by using WithRuntime explicitly.";
            Assert.Contains(expectedMessage, errors);
        }
    }

    [Fact]
    public async Task NotNullRuntimeOnly_Should_Success()
    {
        // Arrange
        var config = new TestConfig3().CreateImmutableConfig();
        var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(DummyBenchmark), config);
        var parameters = new ValidationParameters(runInfo.BenchmarksCases, config);

        // Act
        var errors = await RuntimeValidator.DontFailOnError.ValidateAsync(parameters).Select(e => e.Message).ToArrayAsync();

        // Assert
        Assert.Empty(errors);
    }

    public class DummyBenchmark
    {
        [Benchmark]
        public void Benchmark()
        {
        }
    }

    // TestConfig that expicitly specify runtime.
    private class TestConfig1 : ManualConfig
    {
        public TestConfig1()
        {
            var baseJob = Job.Dry;

            WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());

            AddJob(baseJob.WithToolchain(CsProjCoreToolchain.NetCoreApp80));
            AddJob(baseJob.WithToolchain(CsProjCoreToolchain.NetCoreApp90));
        }
    }

    // TestConfig that contains job that don't specify runtime.
    private class TestConfig2 : ManualConfig
    {
        public TestConfig2()
        {
            var baseJob = Job.Dry;

            WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());

            AddJob(baseJob.WithToolchain(CsProjCoreToolchain.NetCoreApp80));
            AddJob(baseJob.WithToolchain(CsProjCoreToolchain.NetCoreApp90)
                          .WithRuntime(CoreRuntime.Core90));

            // Validate error message for auto generated jobid.
            AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp11_0));
        }
    }

    // TestConfig that expicitly specify runtime.
    private class TestConfig3 : ManualConfig
    {
        public TestConfig3()
        {
            var baseJob = Job.Dry;

            WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());

            AddJob(baseJob.WithToolchain(CsProjCoreToolchain.NetCoreApp80)
                          .WithRuntime(CoreRuntime.Core80)); ;
            AddJob(baseJob.WithToolchain(CsProjCoreToolchain.NetCoreApp90)
                          .WithRuntime(CoreRuntime.Core90));
        }
    }
}
