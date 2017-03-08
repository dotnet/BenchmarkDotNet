using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class PmcDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public PmcDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new PmcDiagnoser());
        }
    }
}