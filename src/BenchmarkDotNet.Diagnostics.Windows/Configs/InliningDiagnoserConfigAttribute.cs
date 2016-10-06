using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class InliningDiagnoserConfigAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public InliningDiagnoserConfigAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new InliningDiagnoser());
        }
    }
}