using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MonoTests : BenchmarkTestExecutor
    {
        public MonoTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("UseMonoRuntime option is available in .NET Core only starting from .NET 6, and it's not supported on Windows+Arm", [EnvRequirement.DotNetCoreOnly, EnvRequirement.NonWindowsArm, EnvRequirement.NonGitHubDraftPR])]
        public void Mono80IsSupported()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry.WithRuntime(MonoCoreRuntime.Net80))
                .WithBuildTimeout(TimeSpan.FromSeconds(240));
            // MonoBenchmark lives in a separate project that targets net8.0, because Mono packages
            // are no longer published for net9.0+ and this project no longer targets net8.0.
            CanExecute<MonoBenchmarks.MonoBenchmark>(config);
        }
    }
}