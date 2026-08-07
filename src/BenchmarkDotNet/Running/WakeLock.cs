using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using Windows.Win32.System.Power;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    public static WakeLockType GetWakeLockType(BenchmarkRunInfo[] benchmarkRunInfos) =>
        benchmarkRunInfos.Length == 0 ? WakeLockType.None : benchmarkRunInfos.Max(static i => i.Config.WakeLock);

    public static IDisposable? Request(WakeLockType wakeLockType, string reason, ILogger logger)
    {
        if (wakeLockType == WakeLockType.None)
            return null;

        if (!OsDetector.IsWindows())
            return null;

        // Must be windows 7 or greater
        if (Environment.OSVersion.Version < new Version(6, 1))
            return null;

        return new WakeLockSentinel(wakeLockType, reason, logger);
    }

    private class WakeLockSentinel : DisposeAtProcessTermination
    {
        private readonly WakeLockType wakeLockType;
        private readonly SafeFileHandle? safePowerHandle;
        private readonly ILogger logger;

        public WakeLockSentinel(WakeLockType wakeLockType, string reason, ILogger logger)
        {
            this.wakeLockType = wakeLockType;
            this.logger = logger;
            try
            {
                safePowerHandle = PowerCreateRequest(reason);
                PowerSetRequest(safePowerHandle, POWER_REQUEST_TYPE.PowerRequestSystemRequired);
                if (wakeLockType == WakeLockType.Display)
                {
                    PowerSetRequest(safePowerHandle, POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
                }
            }
            catch (Win32Exception ex)
            {
                logger.WriteLineError($"Unable to prevent the system from entering sleep or turning off the display (error message: {ex.Message}).");
            }
        }

        protected override void Dispose(bool exiting)
        {
            if (safePowerHandle != null)
            {
                try
                {
                    if (wakeLockType == WakeLockType.Display)
                    {
                        PowerClearRequest(safePowerHandle, POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
                    }
                    PowerClearRequest(safePowerHandle, POWER_REQUEST_TYPE.PowerRequestSystemRequired);
                }
                catch (Win32Exception ex)
                {
                    logger.WriteLineError($"Unable to allow the system from entering sleep or turning off the display (error message: {ex.Message}).");
                }
                safePowerHandle.Dispose();
            }
            base.Dispose(exiting);
        }
    }
}