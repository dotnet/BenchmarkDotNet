using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// determines if all auto-generated files should be kept or removed after running the benchmarks
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class KeepBenchmarkFilesAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public KeepBenchmarkFilesAttribute(bool value = true)
        {
            Config = ManualConfig.CreateEmpty().WithOption(ConfigOptions.KeepBenchmarkFiles, value);
        }
    }
}