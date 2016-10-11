using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class InliningDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public InliningDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new InliningDiagnoser());
        }
    }
}