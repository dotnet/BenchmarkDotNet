using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ThreadingDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        //public ThreadingDiagnoserAttribute() => Config = ManualConfig.CreateEmpty().AddDiagnoser(ThreadingDiagnoser.Default);

        /// <param name="displayLockContentionWhenZero">Display configuration for 'LockContentionCount' when it is empty. True (displayed) by default.</param>
        /// <param name="displayCompletedWorkItemCountWhenZero">Display configuration for 'CompletedWorkItemCount' when it is empty. True (displayed) by default.</param>

        [PublicAPI]
        public ThreadingDiagnoserAttribute(bool displayLockContentionWhenZero = true, bool displayCompletedWorkItemCountWhenZero = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayLockContentionWhenZero, displayCompletedWorkItemCountWhenZero)));
        }
    }
}