using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    public class ArtifactsPathAttribute : Attribute, IConfigSource
    {
        public string Value { get; }
        public IConfig Config { get; }

        public ArtifactsPathAttribute(string value)
        {
            Value = value;
            Config = ManualConfig.CreateEmpty().WithArtifactsPath(value);
        }
    }
}

