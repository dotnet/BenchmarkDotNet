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
        public MemoryDiagnoserAttribute(bool displayGenColumns = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(displayGenColumns)));
        }
    }
}