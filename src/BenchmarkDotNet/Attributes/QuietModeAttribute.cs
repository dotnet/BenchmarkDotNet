using BenchmarkDotNet.Configs;
using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// determines if all auto-generated files should be kept or removed after running the benchmarks
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class QuietModeAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public QuietModeAttribute(bool value = false)
        {
            Config = ManualConfig.CreateEmpty().WithOption(ConfigOptions.QuietMode, value);
        }
    }
}
