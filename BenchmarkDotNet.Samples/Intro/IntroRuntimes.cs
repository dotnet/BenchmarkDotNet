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

        /// <summary>
        /// if your project is targeting multiple frameworks (net40, dnx451, dnxcore50)
        /// and you have dotnet cli installed
        /// you can target all runtimes that you support with single config
        /// </summary>
        private class MultipleRuntimesConfig : ManualConfig
        {
            public MultipleRuntimesConfig()
            {
                Add(Job.Dry.With(Runtime.Clr).With(Jit.RyuJit).With(Jobs.Framework.V40)); // framework for Clr must be set in explicit way
                Add(Job.Dry.With(Runtime.Dnx).With(Jit.RyuJit));
                Add(Job.Dry.With(Runtime.Core).With(Jit.RyuJit));
            }
        }

        [Config(typeof(MultipleRuntimesConfig))]
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