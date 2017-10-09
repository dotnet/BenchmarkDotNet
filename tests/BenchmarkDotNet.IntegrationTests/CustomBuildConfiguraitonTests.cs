using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CsProj;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CustomBuildConfiguraitonTests : BenchmarkTestExecutor
    {
        public CustomBuildConfiguraitonTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void UserCanSpecifyCustomBuildConfiguration()
        {
            var toolchain = RuntimeInformation.IsClassic()
                ? CsProjClassicNetToolchain.Net46 // no support for Roslyn toolchain for this feature
                : CsProjCoreToolchain.Current.Value;

            var jobWithCustomConfiguration = Job.Dry.WithCustomBuildConfiguration("CUSTOM").With(toolchain);
            var config = CreateSimpleConfig(job: jobWithCustomConfiguration);

            CanExecute<CustomBuildConfiguraiton>(config);
        }

        public class CustomBuildConfiguraiton
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