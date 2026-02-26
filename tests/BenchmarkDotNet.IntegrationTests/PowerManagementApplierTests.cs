using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using System.Globalization;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class PowerManagementApplierTests : BenchmarkTestExecutor
    {
        public const string HighPerformancePlanGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        public PowerManagementApplierTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("Setting high-performance plan is suitable only on Windows", EnvRequirement.WindowsOnly)]
        public void TestSettingAndRevertingBackGuid()
        {
            var userPlan = PowerManagementHelper.CurrentPlan;

            using var powerManagementApplier = new PowerManagementApplier(new OutputLogger(Output));

            powerManagementApplier.ApplyPerformancePlan(PowerManagementApplier.Map(PowerPlan.HighPerformance));

            Assert.Equal(HighPerformancePlanGuid, PowerManagementHelper.CurrentPlan.ToString());

            if (CultureInfo.CurrentUICulture.Name == "en-us")
                Assert.Equal("High performance", PowerManagementHelper.CurrentPlanFriendlyName);

            Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
        }

        [FactEnvSpecific("Setting high-performance plan is suitable only on Windows", EnvRequirement.WindowsOnly)]
        public void TestPowerPlanShouldNotChange()
        {
            var userPlan = PowerManagementHelper.CurrentPlan;
            using var powerManagementApplier = new PowerManagementApplier(new OutputLogger(Output));

            powerManagementApplier.ApplyPerformancePlan(PowerManagementApplier.Map(PowerPlan.UserPowerPlan));

            Assert.Equal(userPlan.ToString(), PowerManagementHelper.CurrentPlan.ToString());

            Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
        }

        [FactEnvSpecific("Should keep power plan at Ultimate Performance if current power plan is Ultimate when a power plan is not specifically set", EnvRequirement.WindowsOnly)]
        public void TestKeepingUltimatePowerPlan()
        {
            var ultimateGuid = PowerManagementApplier.Map(PowerPlan.UltimatePerformance);
            var userPlan = PowerManagementHelper.CurrentPlan;

            if (!PowerManagementHelper.PlanExists(ultimateGuid))
            {
                Output.WriteLine("Ultimate Performance plan does not exist or cannot be activated. Skipping test.");
                return;
            }

            PowerManagementHelper.Set(ultimateGuid);

            var job = Job.Default;

            var config = ManualConfig.CreateEmpty().AddJob(job);

            BenchmarkRunner.Run<DummyBenchmark>(config);

            Assert.Equal(ultimateGuid.ToString(), PowerManagementHelper.CurrentPlan.ToString());

            PowerManagementHelper.Set(userPlan.Value);
        }

        public class DummyBenchmark
        {
            [BenchmarkDotNet.Attributes.Benchmark]
            public void DoNothing()
            {
            }
        }
    }
}