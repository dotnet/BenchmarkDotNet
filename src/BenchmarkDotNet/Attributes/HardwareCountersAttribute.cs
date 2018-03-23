using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class HardwareCountersAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constuctor without an array in the argument list
        protected HardwareCountersAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        public HardwareCountersAttribute(params HardwareCounter[] counters)
        {
            Config = ManualConfig.CreateEmpty().With(counters);
        }

        public IConfig Config { get; }
    }
}