using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HideColumnsAttribute : Attribute, IConfigSource
    {
        public string[] Names { get; }

        public IConfig Config { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        protected HideColumnsAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        public HideColumnsAttribute(params string[] names)
        {
            Names = names;
            Config = ManualConfig.CreateEmpty().HideColumns(names);
        }
    }
}