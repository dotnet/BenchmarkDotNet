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

        [FactWindowsOnly("Setting high-performance plan is suitable only on Windows")]
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

        [FactWindowsOnly("Setting high-performance plan is suitable only on Windows")]
        public void TestPowerPlanShouldNotChange()
        {
            var userPlan = PowerManagementHelper.CurrentPlan;
            var powerManagementApplier = new PowerManagementApplier(new OutputLogger(Output));

            powerManagementApplier.ApplyPerformancePlan(PowerManagementApplier.Map(PowerPlan.UserPowerPlan));

            Assert.Equal(userPlan.ToString(), PowerManagementHelper.CurrentPlan.ToString());
            powerManagementApplier.Dispose();

            Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
        }
    }
}
