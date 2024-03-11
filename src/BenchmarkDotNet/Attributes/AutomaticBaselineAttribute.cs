using BenchmarkDotNet.Configs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutomaticBaselineAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public AutomaticBaselineAttribute(AutomaticBaselineMode mode) => Config = ManualConfig.CreateEmpty().WithAutomaticBaseline(mode);
    }
}
