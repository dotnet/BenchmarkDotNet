using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    public class KeepBenchmarkFilesAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public KeepBenchmarkFilesAttribute(bool value = true)
        {
            Config = ManualConfig.CreateEmpty().KeepBenchmarkFiles(value);
        }
    }
}