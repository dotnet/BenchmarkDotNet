using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.Threading;

// *** Attribute Style applied to Assembly ***
[assembly: WakeLock(WakeLockType.System)]

namespace BenchmarkDotNet.Samples;

// *** Attribute Style ***
[WakeLock(WakeLockType.Display)]
public class IntroWakeLock
{
    [Benchmark]
    public void LongRunning() => Thread.Sleep(TimeSpan.FromSeconds(10));
}

// *** Object Style ***
[Config(typeof(Config))]
public class IntroWakeLockObjectStyle
{
    private class Config : ManualConfig
    {
        public Config() => WakeLock = WakeLockType.System;
    }

    [Benchmark]
    public void LongRunning() => Thread.Sleep(TimeSpan.FromSeconds(10));
}
