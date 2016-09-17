using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    public class KeepBenchmarkFilesAttribute : Attribute, IConfigSource
    {
        public bool Value { get; }
        public IConfig Config { get; }

        public KeepBenchmarkFilesAttribute(bool value = true)
        {
            Value = value;
            Config = ManualConfig.CreateEmpty().KeepBenchmarkFiles(value);
        }
    }
}