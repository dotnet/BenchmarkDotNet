using System;
using System.Collections.Generic;
using BenchmarkDotNet.Environments;
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
        private static readonly Dictionary<PowerPlan, string> powerPlansDict = new Dictionary<PowerPlan, string>()
        {
            { PowerPlan.UserPowerPlan, null },
            { PowerPlan.HighPerformance, "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" }
        };

        internal PowerManagementApplier(ILogger logger)
        {
            this.logger = logger;
        }

        internal void ApplyPerformancePlan(PowerPlan powerPlan)
        {
            var guid = powerPlansDict[powerPlan];
            ApplyPerformancePlan(guid);
        }

        internal void ApplyPerformancePlan(string guid)
        {
            if (RuntimeInformation.IsWindows())
            {
                if (string.IsNullOrEmpty(guid) == false && powerPlanChanged == false)
                    ApplyPlanByGuid(guid);
                else if (string.IsNullOrEmpty(guid))
                    ApplyUserPowerPlan();
            }
        }

        internal void ApplyUserPowerPlan()
        {
            if (powerPlanChanged && RuntimeInformation.IsWindows())
            {
                try
                {
                    if (userCurrentPowerPlan != null && PowerManagementHelper.Set(userCurrentPowerPlan.Value))
                    {
                        powerPlanChanged = false;
                        var powerPlanFriendlyName = PowerManagementHelper.CurrentPlanFriendlyName;
                        logger.WriteLineInfo($"Successfully reverted power plan (GUID: {userCurrentPowerPlan.Value} FriendlyName: {powerPlanFriendlyName})");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLineError($"Cannot revert power plan (error message: {ex.Message})");
                }
            }
        }

        private void ApplyPlanByGuid(string guid)
        {
            try
            {
                if (isInitialized == false)
                {
                    userCurrentPowerPlan = PowerManagementHelper.CurrentPlan;
                    isInitialized = true;
                }

                if (PowerManagementHelper.Set(new Guid(guid)))
                {
                    powerPlanChanged = true;
                    var powerPlanFriendlyName = PowerManagementHelper.CurrentPlanFriendlyName;
                    logger.WriteInfo($"Setup power plan (GUID: {guid} FriendlyName: {powerPlanFriendlyName})");
                }
                else
                    logger.WriteLineError($"Cannot setup power plan (GUID: {guid})");
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"Cannot setup power plan (GUID: {guid}, error message: {ex.Message})");
            }
        }
    }
}
