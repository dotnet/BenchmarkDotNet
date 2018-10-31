using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BuildTimeoutTests: BenchmarkTestExecutor
    {
        public BuildTimeoutTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact]
        public void WhenBuildTakesMoreTimeThanTheTimeoutTheEntireProcessTreeIsKilled()
        {
            if (RuntimeInformation.GetCurrentPlatform() == Platform.X86) // CoreRT does not support 32bit yet
                return;
            
            // we use CoreRT on purpose because it takes a LOT of time to build it
            // so we can be sure that timeout = 1s should fail!
            var timeout = TimeSpan.FromSeconds(1);
            
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry
                    .With(Runtime.CoreRT)
                    .With(CoreRtToolchain.CreateBuilder()
                        .UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-26414-01") // we test against specific version to keep this test stable
                        .Timeout(timeout)
                        .ToToolchain()));

            var processesBefore = Process.GetProcesses();
            var summary = CanExecute<CoreRtBenchmark>(config, fullValidation: false);
            var processesAfter = Process.GetProcesses();

            Assert.All(summary.Reports, report => Assert.False(report.BuildResult.IsBuildSuccess));
            Assert.All(summary.Reports, report => Assert.Contains("The configured timeout", report.BuildResult.BuildException.Message));
            Assert.True(CountOfProcessesWeCareAbout(processesAfter) <= CountOfProcessesWeCareAbout(processesBefore)); // CI or the VM could have spawn something in the meantime, but let's hope for the best. Remove in the case the test is not stable 
        }

        private static int CountOfProcessesWeCareAbout(Process[] processes)
            => processes.Count(process =>
                process.ProcessName.StartsWith("dotnet", StringComparison.InvariantCultureIgnoreCase) ||
                process.ProcessName.StartsWith("msbuild", StringComparison.InvariantCultureIgnoreCase));
    }

    public class Impossible
    {
        [Benchmark]
        public void Check() => Environment.FailFast("This benchmark should have been never executed because 1s is not enough to build CoreRT benchmark!");
    }
}