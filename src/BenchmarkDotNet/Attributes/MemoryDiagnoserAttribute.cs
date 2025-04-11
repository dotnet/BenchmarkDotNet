using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        /// <param name="displayGenColumns">Display Garbage Collections per Generation columns (Gen 0, Gen 1, Gen 2). True by default.</param>
        /// <param name="includeSurvived">If true, monitoring will be enabled and survived memory will be measured on the first benchmark run.</param>
        public MemoryDiagnoserAttribute(bool displayGenColumns = true, bool includeSurvived = false)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(displayGenColumns, includeSurvived)));
        }
    }
}