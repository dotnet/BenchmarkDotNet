using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
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
