using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.Mono;

namespace BenchmarkDotNet.IntegrationTests;

public class IntegrationTestSetupTests
{
    [Fact]
    public void IntegrationTestsAreDetected() => Assert.True(XUnitHelper.IsIntegrationTest.Value);

    [Theory]
    [InlineData("net10.0", "default", true)]
    [InlineData("net10.0", "configuration", false)]
    [InlineData("net10.0", "arguments", false)]
    [InlineData("r2r10.0", "default", false)]
    [InlineData("r2r10.0", "configuration", false)]
    [InlineData("r2r10.0", "arguments", false)]
    [InlineData("mono10.0", "default", false)]
    [InlineData("nativeaot10.0", "default", false)]
    [InlineData("monowasm10.0", "default", false)]
    [InlineData("monowasmaot10.0", "default", false)]
    [InlineData("corewasm11.0", "default", false)]
    [InlineData("CoreRun", "default", false)]
    [InlineData("InProcess", "default", false)]
    public void OnlyNonPublishingCliBuildsUseTheNoDependenciesShortcut(string toolchain, string buildCharacteristic, bool usesShortcut)
    {
        var job = toolchain switch
        {
            "mono10.0" => Job.Dry.WithToolchain(CsProjMonoCoreToolchain.Mono10_0),
            "CoreRun" => Job.Dry.WithToolchain(CoreRunToolchain.From(new CoreRunSettings
            {
                // This case inspects selection only; no CoreRun executable is needed or executed.
                SourceCoreRun = new FileInfo(typeof(IntegrationTestSetupTests).Assembly.Location),
                CreateCopy = false,
                TargetFrameworkMoniker = "net10.0"
            })),
            "InProcess" => Job.Dry.WithToolchain(InProcessNoEmitToolchain.Default),
            _ => Job.Dry.WithRuntime(Runtime.Parse(toolchain))
        };
        job = buildCharacteristic switch
        {
            "default" => job,
            "configuration" => job.WithCustomBuildConfiguration(InfrastructureMode.ReleaseConfigurationName),
            "arguments" => job.WithArguments([]),
            _ => throw new ArgumentOutOfRangeException(nameof(buildCharacteristic))
        };
        var config = ManualConfig.CreateEmpty().CreateImmutableConfig();
        var benchmark = BenchmarkCase.Create(new Descriptor(MockFactory.MockType, MockFactory.MockMethodInfo),
            job, ParameterInstances.Empty, config);
        var partition = new BuildPartition([new BenchmarkBuildInfo(benchmark, config, 0, new([]))], BenchmarkRunnerClean.DefaultResolver);

        Assert.Equal(usesShortcut && RuntimeInformation.IsNetCore, partition.ForcedNoDependenciesForIntegrationTests);
    }
}
