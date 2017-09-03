using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
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
            var toolchain =
#if CLASSIC
                CsProjClassicNetToolchain.Net46; // no support for Roslyn toolchain for this feature
#elif CORE
                CsProjCoreToolchain.Current.Value;
#endif

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