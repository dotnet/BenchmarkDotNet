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

    public static IDisposable Request(WakeLockType wakeLockType, string reason) =>
        wakeLockType == WakeLockType.No || !OsDetector.IsWindows() ? null : new WakeLockSentinel(wakeLockType, reason);

    private class WakeLockSentinel : DisposeAtProcessTermination
    {
        private readonly WakeLockType wakeLockType;
        private readonly SafePowerHandle safePowerHandle;

        public WakeLockSentinel(WakeLockType wakeLockType, string reason)
        {
            this.wakeLockType = wakeLockType;
            safePowerHandle = PInvoke.PowerCreateRequest(reason);
            PInvoke.PowerSetRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestSystemRequired);
            if (wakeLockType == WakeLockType.RequireDisplay)
            {
                PInvoke.PowerSetRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
            }
        }

        public override void Dispose()
        {
            if (wakeLockType == WakeLockType.RequireDisplay)
            {
                PInvoke.PowerClearRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestDisplayRequired);
            }
            PInvoke.PowerClearRequest(safePowerHandle, PInvoke.POWER_REQUEST_TYPE.PowerRequestSystemRequired);
            safePowerHandle.Dispose();
            base.Dispose();
        }
    }
}