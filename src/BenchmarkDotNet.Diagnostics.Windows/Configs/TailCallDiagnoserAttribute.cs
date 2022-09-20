using BenchmarkDotNet.Configs;
using System;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TailCallDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        /// <param name="logFailuresOnly">only the methods that failed to get tail called. True by default.</param>
        /// <param name="filterByNamespace">only the methods from declaring type's namespace. Set to false if you want to see all Jit tail events. True by default.</param>
        public TailCallDiagnoserAttribute(bool logFailuresOnly = true, bool filterByNamespace = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new TailCallDiagnoser(logFailuresOnly, filterByNamespace));
        }
    }
}
