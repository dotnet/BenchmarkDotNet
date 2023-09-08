using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// determines if benchmark should be run after being generated and built
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateAndBuildOnlyAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public GenerateAndBuildOnlyAttribute(bool value = true)
        {
            Config = ManualConfig.CreateEmpty().WithOption(ConfigOptions.GenerateAndBuildOnly, value);
        }
    }
}