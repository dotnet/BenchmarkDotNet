using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class PowerManagementApplierTests : BenchmarkTestExecutor
    {
        public const string PowerSaverGuid = "e22fd527-0c09-43ad-83d6-ba300affc27d";

        public PowerManagementApplierTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TestSettingAndRevertingBackGuid()
        {
            if (RuntimeInformation.IsWindows())
            {
                var powerManagementApplier = new PowerManagementApplier();
                var userPlan = PowerManagementHelper.CurrentPlan;
                var logger = new OutputLogger(Output);
                var config = DefaultConfig.Instance.With(logger);
                powerManagementApplier.ApplyPerformancePlan(logger, config.HighPerformancePowerPlan);
                Assert.Equal(PowerManagementHelper.HighPerformanceGuid, PowerManagementHelper.CurrentPlan.ToString());
                Assert.Equal("High performance", PowerManagementHelper.CurrentPlanFriendlyName);
                powerManagementApplier.ApplyUserPowerPlan(logger);
                Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
            }
        }

        [Fact]
        public void TestPowerPlanShouldNotChange()
        {
            if (RuntimeInformation.IsWindows())
            {
                var powerManagementApplier = new PowerManagementApplier();
                var userPlan = PowerManagementHelper.CurrentPlan;
                var logger = new OutputLogger(Output);
                var config = DefaultConfig.Instance.With(logger).WithHighPerformancePowerPlan(false);
                powerManagementApplier.ApplyPerformancePlan(logger, config.HighPerformancePowerPlan);
                Assert.Equal(userPlan.ToString(), PowerManagementHelper.CurrentPlan.ToString());
                powerManagementApplier.ApplyUserPowerPlan(logger);
                Assert.Equal(userPlan, PowerManagementHelper.CurrentPlan);
            }
        }
    }
}
