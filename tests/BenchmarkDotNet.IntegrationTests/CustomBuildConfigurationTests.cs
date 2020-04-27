using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CustomBuildConfigurationTests : BenchmarkTestExecutor
    {
        public CustomBuildConfigurationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void UserCanSpecifyCustomBuildConfiguration()
        {
            var jobWithCustomConfiguration = Job.Dry.WithCustomBuildConfiguration("CUSTOM");

            var config = CreateSimpleConfig(job: jobWithCustomConfiguration);

            CanExecute<CustomBuildConfiguration>(config);
        }

        public class CustomBuildConfiguration
        {
            [Benchmark]
            public void Benchmark()
            {
#if !CUSTOM
                throw new InvalidOperationException("Should never happen");
#endif
            }
        }
    }
}