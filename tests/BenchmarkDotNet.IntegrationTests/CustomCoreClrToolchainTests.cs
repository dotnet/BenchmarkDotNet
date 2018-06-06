using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.CustomCoreClr;
using Xunit.Abstractions;
// ReSharper disable InconsistentNaming we use crazy names for the types to include the  version numbers

namespace BenchmarkDotNet.IntegrationTests
{
    public class CustomCoreClrToolchainTests : BenchmarkTestExecutor
    {
        private const string WeRunTheseTestsForNetCoreOnlyBecauseTheyTakeALotOfTime =
            "We run it only for .NET Core, it takes too long to run it for all frameworks";

        public CustomCoreClrToolchainTests(ITestOutputHelper output) : base(output) { }

        [FactDotNetCoreOnly(skipReason: WeRunTheseTestsForNetCoreOnlyBecauseTheyTakeALotOfTime)]
        public void CanBenchmarkGivenCoreFxMyGetBuild()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreClrDefault()
                        .UseCoreFxNuGet("4.5.0-rtm-26531-02")
                        .ToToolchain()));

            CanExecute<Check_4_6_26531_02_CoreFxVersion>(config);
        }

        public class Check_4_6_26531_02_CoreFxVersion : CheckCoreClrAndCoreFxVersions
        {
            public Check_4_6_26531_02_CoreFxVersion() : base(expectedCoreFxVersion: "4.6.26531.02") { }
        }

        [FactDotNetCoreOnly(skipReason: WeRunTheseTestsForNetCoreOnlyBecauseTheyTakeALotOfTime)]
        public void CanBenchmarkGivenCoreClrMyGetBuild()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxDefault()
                        .UseCoreClrNuGet("2.1.0-rtm-26528-02")
                        .ToToolchain()));

            CanExecute<Check_4_6_26528_02_CoreClrVersion>(config);
        }

        public class Check_4_6_26528_02_CoreClrVersion : CheckCoreClrAndCoreFxVersions
        {
            public Check_4_6_26528_02_CoreClrVersion() : base(expectedCoreClrVersion: "4.6.26528.02") { }
        }

        [FactDotNetCoreOnly(skipReason: WeRunTheseTestsForNetCoreOnlyBecauseTheyTakeALotOfTime)]
        public void CanBenchmarkGivenCoreClrAndCoreFxMyGetBuilds()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxNuGet("4.5.0-rtm-26531-02")
                        .UseCoreClrNuGet("2.1.0-rtm-26528-02")
                        .ToToolchain()));

            CanExecute<Check_4_6_6528_02_CoreClrAnd_4_6_26531_02_CoreFxVersions>(config);
        }

        public class Check_4_6_6528_02_CoreClrAnd_4_6_26531_02_CoreFxVersions : CheckCoreClrAndCoreFxVersions
        {
            public Check_4_6_6528_02_CoreClrAnd_4_6_26531_02_CoreFxVersions() : base(expectedCoreClrVersion: "4.6.26528.02", expectedCoreFxVersion: "4.6.26531.02") { }
        }
    }
}