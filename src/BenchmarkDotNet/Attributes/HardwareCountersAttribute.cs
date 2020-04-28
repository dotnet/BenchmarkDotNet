using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class HardwareCountersAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        protected HardwareCountersAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        public HardwareCountersAttribute(params HardwareCounter[] counters)
        {
            Config = ManualConfig.CreateEmpty().AddHardwareCounters(counters.Select(x => (HardwareCounterInfo)x).ToArray());
        }

        public HardwareCountersAttribute(params string[] counters)
        {
            Config = ManualConfig.CreateEmpty().AddHardwareCounters(counters.Select(x => new HardwareCounterInfo(x)).ToArray());
        }

        public IConfig Config { get; }
    }
}