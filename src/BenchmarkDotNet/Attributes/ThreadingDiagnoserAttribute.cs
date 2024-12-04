using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ThreadingDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        /// <param name="displayWorkItemsColumn">Display Work Items column. True by default.</param>
        /// <param name="displayLockContentionsColumn">Display Lock Contentions column. True by default.</param>
        public ThreadingDiagnoserAttribute(bool displayWorkItemsColumn = true, bool displayLockContentionsColumn = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayWorkItemsColumn, displayLockContentionsColumn)));
        }
    }
}