using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class FailingProcessSpawnTests : BenchmarkTestExecutor
    {
        public FailingProcessSpawnTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void NoHangs()
        {
            Platform wrongPlatform = RuntimeInformation.GetCurrentPlatform() switch
            {
                Platform.X64 or Platform.X86 => Platform.Arm64,
                _ => Platform.X64
            };

            if (wrongPlatform == Platform.X64 && RuntimeInformation.IsFullFramework)
            {
                // It seems full Framework on Arm ignores the platform and simply runs the native platform, causing this test to fail.
                return;
            }

            var invalidPlatformJob = Job.Dry.WithPlatform(wrongPlatform);
            var config = CreateSimpleConfig(job: invalidPlatformJob);

            var summary = CanExecute<Simple>(config, fullValidation: false);

            var executeResults = summary.Reports.Single().ExecuteResults.Single();

            Assert.True(executeResults.FoundExecutable);
            Assert.False(executeResults.IsSuccess);
            Assert.NotEqual(0, executeResults.ExitCode);
        }

        public class Simple
        {
            [Benchmark]
            public void DoNothing() { }
        }
    }
}
