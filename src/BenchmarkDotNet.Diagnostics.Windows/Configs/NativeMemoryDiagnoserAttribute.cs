using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class NativeMemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public NativeMemoryDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new NativeMemoryDiagnoser());
        }

        public IConfig Config { get; }
    }
}