using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class MemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public MemoryDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new MemoryDiagnoser());
        }
    }
}