using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class HideColumnsAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        protected HideColumnsAttribute() => Config = ManualConfig.CreateEmpty();

        public HideColumnsAttribute(params string[] names) => Config = ManualConfig.CreateEmpty().HideColumns(names);
    }
}