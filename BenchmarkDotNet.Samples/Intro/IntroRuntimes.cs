using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroRuntimes
    {
        private const string AvoidParsingException = "//";

        /// <summary>
        /// when Runtime is not set, then the default runtime is used - Host
        /// depending on your target .Net framework compiler will pick up the right BenchmarkDotNet.dll
        ///   * for .NET 4.0 - 4.6 the one with Clr as Default or Mono if this is Mono
        ///   * for dnx451 the one with Dnx as Default
        ///   * for dnxcore50 the one with Core as Default
        /// </summary>
        public class IntroDefaultToolchain
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine($"{AvoidParsingException} {Toolchain.GetToolchain(Runtime.Host)}");
            }
        }

        private class DnxAndCoreConfig : ManualConfig
        {
            public DnxAndCoreConfig()
            {
                Add(Job.Dry.With(Runtime.Dnx).With(Jit.Host));
                Add(Job.Dry.With(Runtime.Core).With(Jit.Host));
            }
        }

        [Config(typeof(DnxAndCoreConfig))]
        public class IntroMultipleRuntimes
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine($"{AvoidParsingException} {Toolchain.GetToolchain(Runtime.Host)}");
            }
        }
    }
}