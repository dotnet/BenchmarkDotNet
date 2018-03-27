using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
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
                        .UseCoreFxNuGet("4.5.0-preview3-26328-01")
                        .ToToolchain()));

            CanExecute<Check_4_6_26328_01_CoreFxVersion>(config);
        }

        public class Check_4_6_26328_01_CoreFxVersion : CheckCoreClrAndCoreFxVersions
        {
            public Check_4_6_26328_01_CoreFxVersion() : base(expectedCoreFxVersion: "4.6.26328.01") { }
        }

        [FactDotNetCoreOnly(skipReason: WeRunTheseTestsForNetCoreOnlyBecauseTheyTakeALotOfTime)]
        public void CanBenchmarkGivenCoreClrMyGetBuild()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxDefault()
                        .UseCoreClrNuGet("2.1.0-preview3-26329-08")
                        .ToToolchain()));

            CanExecute<Check_4_6_26329_08_CoreClrVersion>(config);
        }

        public class Check_4_6_26329_08_CoreClrVersion : CheckCoreClrAndCoreFxVersions
        {
            public Check_4_6_26329_08_CoreClrVersion() : base(expectedCoreClrVersion: "4.6.26329.08") { }
        }

        [FactDotNetCoreOnly(skipReason: WeRunTheseTestsForNetCoreOnlyBecauseTheyTakeALotOfTime)]
        public void CanBenchmarkGivenCoreClrAndCoreFxMyGetBuilds()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry.With(
                    CustomCoreClrToolchain.CreateBuilder()
                        .UseCoreFxNuGet("4.5.0-preview3-26330-06")
                        .UseCoreClrNuGet("2.1.0-preview3-26329-08")
                        .ToToolchain()));

            CanExecute<Check_4_6_26329_08_CoreClrAnd_4_6_26330_06_CoreFxVersions>(config);
        }

        public class Check_4_6_26329_08_CoreClrAnd_4_6_26330_06_CoreFxVersions : CheckCoreClrAndCoreFxVersions
        {
            public Check_4_6_26329_08_CoreClrAnd_4_6_26330_06_CoreFxVersions() : base(expectedCoreClrVersion: "4.6.26329.08", expectedCoreFxVersion: "4.6.26330.06") { }
        }
    }
}