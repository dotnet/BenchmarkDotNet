using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Helpers;
using System;
using System.Linq;

namespace BenchmarkDotNet.Running;

internal partial class WakeLock
{
    public static WakeLockType GetWakeLockType(BenchmarkRunInfo[] benchmarkRunInfos) =>
        benchmarkRunInfos.Select(static i => i.Config.WakeLock).Max();

    private static readonly bool OsVersionIsSupported =
        // Must be windows 7 or greater
        OsDetector.IsWindows() && Environment.OSVersion.Version >= new Version(6, 1);

    public static IDisposable Request(WakeLockType wakeLockType, string reason) =>
        wakeLockType == WakeLockType.None || !OsVersionIsSupported ? null : new WakeLockSentinel(wakeLockType, reason);

    private class WakeLockSentinel : DisposeAtProcessTermination
    {
        private readonly WakeLockType wakeLockType;
        private readonly SafePowerHandle safePowerHandle;

        public WakeLockSentinel(WakeLockType wakeLockType, string reason)
        {
            this.wakeLockType = wakeLockType;
            safePowerHandle = PInvoke.PowerCreateRequest(reason);
            PInvoke.PowerSetRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestSystemRequired);
            if (wakeLockType == WakeLockType.Display)
            {
                PInvoke.PowerSetRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
            }
        }

        public override void Dispose()
        {
            if (wakeLockType == WakeLockType.Display)
            {
                PInvoke.PowerClearRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
            }
            PInvoke.PowerClearRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestSystemRequired);
            safePowerHandle.Dispose();
            base.Dispose();
        }
    }
}