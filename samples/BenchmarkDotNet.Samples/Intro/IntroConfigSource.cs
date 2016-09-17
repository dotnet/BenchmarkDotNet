using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [MyConfigSource(Jit.LegacyJit, Jit.RyuJit)]
    public class IntroConfigSource
    {
        /// <summary>
        /// Dry-x64 jobs for specific jits
        /// </summary>
        private class MyConfigSourceAttribute : Attribute, IConfigSource
        {
            public IConfig Config { get; private set; }

            public MyConfigSourceAttribute(params Jit[] jits)
            {
                var jobs = jits.Select(jit => Job.Dry.With(Platform.X64).With(jit)).ToArray();
                Config = ManualConfig.CreateEmpty().With(jobs);
            }
        }

        [Benchmark]
        public void Foo()
        {
            Thread.Sleep(10);
        }
    }
}