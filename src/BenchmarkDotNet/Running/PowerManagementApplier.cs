using System;
using System.Collections.Generic;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal class PowerManagementApplier : IDisposable
    {
        private static readonly Guid UserPowerPlan = new Guid("67b4a053-3646-4532-affd-0535c9ea82a7");

        private static readonly Dictionary<PowerPlan, Guid> PowerPlansDict = new Dictionary<PowerPlan, Guid>()
        {
            { PowerPlan.UserPowerPlan, UserPowerPlan },
            { PowerPlan.HighPerformance, new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c") },
            { PowerPlan.PowerSaver, new Guid("a1841308-3541-4fab-bc81-f71556f20b4a") },
            { PowerPlan.Balanced, new Guid("381b4222-f694-41f0-9685-ff5bb260df2e") },
            { PowerPlan.UltimatePerformance, new Guid("e9a42b02-d5df-448d-aa00-03f14749eb61") },
        };

        private readonly ILogger logger;
        private Guid? userCurrentPowerPlan;
        private bool powerPlanChanged;
        private bool isInitialized;

        internal PowerManagementApplier(ILogger logger) => this.logger = logger;

        public void Dispose() => ApplyUserPowerPlan();

        internal static Guid Map(PowerPlan value) => PowerPlansDict[value];

        internal void ApplyPerformancePlan(Guid id)
        {
            if (!RuntimeInformation.IsWindows() || id == Guid.Empty)
                return;

            if (id != UserPowerPlan)
                ApplyPlanByGuid(id);
            else
                ApplyUserPowerPlan();
        }

        private void ApplyUserPowerPlan()
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

        private void ApplyPlanByGuid(Guid guid)
        {
            try
            {
                if (isInitialized == false)
                {
                    userCurrentPowerPlan = PowerManagementHelper.CurrentPlan;
                    isInitialized = true;
                }

                if (PowerManagementHelper.Set(guid))
                {
                    powerPlanChanged = true;
                    var powerPlanFriendlyName = PowerManagementHelper.CurrentPlanFriendlyName;
                    logger.WriteLineInfo($"Setup power plan (GUID: {guid} FriendlyName: {powerPlanFriendlyName})");
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
