using System;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [MyConfigSource(Jit.LegacyJit, Jit.RyuJit)]
    public class IntroConfigEncoding
    {
        /// <summary>
        /// Dry-x64 jobs for specific jits
        /// </summary>
        private class MyConfigSourceAttribute : Attribute, IConfigSource
        {
            public IConfig Config { get; private set; }

            public MyConfigSourceAttribute(params Jit[] jits)
            {
                var jobs = jits
                    .Select(jit => new Job(Job.Dry) { Env = { Jit = jit, Platform = Platform.X64 } })
                    .ToArray();
                Config = ManualConfig.CreateEmpty().With(jobs).With(Encoding.Unicode);
            }
        }

        [Benchmark]
        public double Foo()
        {
            return Math.Sqrt(2);
        }
    }
}