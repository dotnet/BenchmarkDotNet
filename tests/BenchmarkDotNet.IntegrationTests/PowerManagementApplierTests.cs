using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
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
            var powerManagementApplier = new PowerManagementApplier(new OutputLogger(Output));

            powerManagementApplier.ApplyPerformancePlan(PowerManagementApplier.Map(PowerPlan.HighPerformance));

            Assert.Equal(HighPerformancePlanGuid, PowerManagementHelper.CurrentPlan.ToString());
            Assert.Equal("High performance", PowerManagementHelper.CurrentPlanFriendlyName);
            powerManagementApplier.Dispose();

            Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
        }

        [FactEnvSpecific("Setting high-performance plan is suitable only on Windows", EnvRequirement.WindowsOnly)]
        public void TestPowerPlanShouldNotChange()
        {
            var userPlan = PowerManagementHelper.CurrentPlan;
            var powerManagementApplier = new PowerManagementApplier(new OutputLogger(Output));

            powerManagementApplier.ApplyPerformancePlan(PowerManagementApplier.Map(PowerPlan.UserPowerPlan));

            Assert.Equal(userPlan.ToString(), PowerManagementHelper.CurrentPlan.ToString());
            powerManagementApplier.Dispose();

            Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
        }

        [FactEnvSpecific("Should change to High Performance if user requests it and High Performance plan is present, even if currently on Ultimate Performance", EnvRequirement.WindowsOnly)]
        public void ShouldSwitchToHighPerformanceIfPresentWhenRequestedEvenIfOnUltimate()
        {
            var ultimateGuid = PowerManagementApplier.Map(PowerPlan.UltimatePerformance);
            var highGuid = PowerManagementApplier.Map(PowerPlan.HighPerformance);
            var userPlan = PowerManagementHelper.CurrentPlan;

            var logger = new OutputLogger(Output);
            var powerManagementApplier = new PowerManagementApplier(logger);

            PowerManagementHelper.Set(ultimateGuid);

            powerManagementApplier.ApplyPerformancePlan(highGuid);

            Assert.Equal(highGuid.ToString(), PowerManagementHelper.CurrentPlan.ToString());

            PowerManagementHelper.Set(userPlan.Value);
            powerManagementApplier.Dispose();
        }

        [FactEnvSpecific("Should not change plan if already High Performance", EnvRequirement.WindowsOnly)]
        public void ShouldNotChangeIfAlreadyHighPerformance()
        {
            var highGuid = PowerManagementApplier.Map(PowerPlan.HighPerformance);
            var userPlan = PowerManagementHelper.CurrentPlan;

            var logger = new OutputLogger(Output);
            var powerManagementApplier = new PowerManagementApplier(logger);

            PowerManagementHelper.Set(highGuid);

            powerManagementApplier.ApplyPerformancePlan(highGuid);

            Assert.Equal(highGuid.ToString(), PowerManagementHelper.CurrentPlan.ToString());

            PowerManagementHelper.Set(userPlan.Value);
            powerManagementApplier.Dispose();
        }
    }
}