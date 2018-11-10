using System;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal class PowerManagementApplier
    {
        private Guid? _userCurrentPowerPlan;
        private bool _powerPlanChanged = false;
        private bool _isInitialized = false;

        internal void ApplyPerformancePlan(ILogger logger, bool highPerformancePowerPlan)
        {
            if (RuntimeInformation.IsWindows())
            {
                if (highPerformancePowerPlan && _powerPlanChanged == false)
                    ApplyHighPerformancePlan(logger);
                else if (highPerformancePowerPlan == false)
                    ApplyUserPowerPlan(logger);
            }
        }

        internal void ApplyUserPowerPlan(ILogger logger)
        {
            if (_powerPlanChanged && _userCurrentPowerPlan != null && RuntimeInformation.IsWindows())
            {
                try
                {
                    if (_userCurrentPowerPlan != null && PowerManagementHelper.Set(_userCurrentPowerPlan.Value))
                    {
                        _powerPlanChanged = false;
                        var powerPlanFriendlyName = PowerManagementHelper.CurrentPlanFriendlyName;
                        logger.WriteInfo($"Succesfully reverted power plan (GUID: {_userCurrentPowerPlan.Value} FriendlyName: {powerPlanFriendlyName})");
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
                if (_isInitialized == false)
                {
                    _userCurrentPowerPlan = PowerManagementHelper.CurrentPlan;
                    _isInitialized = true;
                }

                if (PowerManagementHelper.Set(new Guid(PowerManagementHelper.HighPerformanceGuid)))
                {
                    _powerPlanChanged = true;
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
