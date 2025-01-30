using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using System;
using System.ComponentModel;
using System.Linq;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    public static WakeLockType GetWakeLockType(BenchmarkRunInfo[] benchmarkRunInfos) =>
        benchmarkRunInfos.Select(static i => i.Config.WakeLock).Max();

    private static readonly bool OsVersionIsSupported =
        // Must be windows 7 or greater
        OsDetector.IsWindows() && Environment.OSVersion.Version >= new Version(6, 1);

    public static IDisposable? Request(WakeLockType wakeLockType, string reason, ILogger logger) =>
        wakeLockType == WakeLockType.None || !OsVersionIsSupported ? null : new WakeLockSentinel(wakeLockType, reason, logger);

    private class WakeLockSentinel : DisposeAtProcessTermination
    {
        private readonly WakeLockType wakeLockType;
        private readonly SafePowerHandle? safePowerHandle;
        private readonly ILogger logger;

        public WakeLockSentinel(WakeLockType wakeLockType, string reason, ILogger logger)
        {
            this.wakeLockType = wakeLockType;
            this.logger = logger;
            try
            {
                safePowerHandle = PInvoke.PowerCreateRequest(reason);
                PInvoke.PowerSetRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestSystemRequired);
                if (wakeLockType == WakeLockType.Display)
                {
                    PInvoke.PowerSetRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
                }
            }
            catch (Win32Exception ex)
            {
                logger.WriteLineError($"Unable to prevent the system from entering sleep or turning off the display (error message: {ex.Message}).");
            }
        }

        public override void Dispose()
        {
            if (safePowerHandle != null)
            {
                try
                {
                    if (wakeLockType == WakeLockType.Display)
                    {
                        PInvoke.PowerClearRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
                    }
                    PInvoke.PowerClearRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestSystemRequired);
                }
                catch (Win32Exception ex)
                {
                    logger.WriteLineError($"Unable to allow the system from entering sleep or turning off the display (error message: {ex.Message}).");
                }
                safePowerHandle.Dispose();
            }
            base.Dispose();
        }
    }
}