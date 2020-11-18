using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public MemoryDiagnoserAttribute(bool includeSurvived = false)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(includeSurvived ? MemoryDiagnoser.WithSurvived : MemoryDiagnoser.Default);
        }
    }
}