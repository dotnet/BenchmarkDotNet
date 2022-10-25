using BenchmarkDotNet.Configs;
using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Run benchmars in quiet mode.
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class QuietModeAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public QuietModeAttribute(bool value = false)
        {
            Config = ManualConfig.CreateEmpty().WithOption(ConfigOptions.QuietMode, value);
        }
    }
}
