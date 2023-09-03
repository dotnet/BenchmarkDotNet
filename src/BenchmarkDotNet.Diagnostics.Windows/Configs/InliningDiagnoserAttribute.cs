using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InliningDiagnoserAttribute : Attribute, IConfigSource
    {
        /// <param name="logFailuresOnly">only the methods that failed to get inlined. True by default.</param>
        /// <param name="filterByNamespace">only the methods from declaring type's namespace. Set to false if you want to see all Jit inlining events. True by default.</param>
        public InliningDiagnoserAttribute(bool logFailuresOnly = true, bool filterByNamespace = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new InliningDiagnoser(logFailuresOnly, filterByNamespace));
        }

        public InliningDiagnoserAttribute(bool logFailuresOnly = true, string[]? allowedNamespaces = null)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new InliningDiagnoser(logFailuresOnly, allowedNamespaces));
        }

        public IConfig Config { get; }
    }
}