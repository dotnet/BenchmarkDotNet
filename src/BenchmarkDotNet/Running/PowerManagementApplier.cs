using System;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal class PowerManagementApplier
    {
        private readonly ILogger logger;
        private Guid? userCurrentPowerPlan;
        private bool powerPlanChanged = false;
        private bool isInitialized = false;

        internal PowerManagementApplier(ILogger logger)
        {
            this.logger = logger;
        }

        internal void ApplyPerformancePlan(bool highPerformancePowerPlan)
        {
            if (RuntimeInformation.IsWindows())
            {
                if (highPerformancePowerPlan && powerPlanChanged == false)
                    ApplyHighPerformancePlan(logger);
                else if (highPerformancePowerPlan == false)
                    ApplyUserPowerPlan(logger);
            }
        }

        internal void ApplyUserPowerPlan(ILogger logger)
        {
            if (powerPlanChanged && userCurrentPowerPlan != null && RuntimeInformation.IsWindows())
            {
                try
                {
                    if (userCurrentPowerPlan != null && PowerManagementHelper.Set(userCurrentPowerPlan.Value))
                    {
                        powerPlanChanged = false;
                        var powerPlanFriendlyName = PowerManagementHelper.CurrentPlanFriendlyName;
                        logger.WriteInfo($"Succesfully reverted power plan (GUID: {userCurrentPowerPlan.Value} FriendlyName: {powerPlanFriendlyName})");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLineError($"Cannot revert power plan (error message: {ex.Message})");
                }
            }
        }

        private void ApplyHighPerformancePlan(ILogger logger)
        {
            try
            {
                if (isInitialized == false)
                {
                    userCurrentPowerPlan = PowerManagementHelper.CurrentPlan;
                    isInitialized = true;
                }

                if (PowerManagementHelper.Set(new Guid(PowerManagementHelper.HighPerformanceGuid)))
                {
                    powerPlanChanged = true;
                    var powerPlanFriendlyName = PowerManagementHelper.CurrentPlanFriendlyName;
                    logger.WriteInfo($"Setup power plan (GUID: {PowerManagementHelper.HighPerformanceGuid} FriendlyName: {powerPlanFriendlyName})");
                }
                else
                    logger.WriteLineError($"Cannot setup power plan (GUID: {PowerManagementHelper.HighPerformanceGuid})");
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"Cannot setup power plan (GUID: {PowerManagementHelper.HighPerformanceGuid}, error message: {ex.Message})");
            }
        }
    }
}
