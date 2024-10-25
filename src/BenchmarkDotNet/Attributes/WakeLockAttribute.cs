using BenchmarkDotNet.Configs;
using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Placing a <see cref="WakeLockAttribute"/> on your assembly or class controls whether the
    /// system enters sleep or turns off the display while benchmarks run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    public sealed class WakeLockAttribute : Attribute, IConfigSource
    {
        public WakeLockAttribute(WakeLockType wakeLockType) =>
            Config = ManualConfig.CreateEmpty().WithWakeLock(wakeLockType);

        public IConfig Config { get; }
    }
}
