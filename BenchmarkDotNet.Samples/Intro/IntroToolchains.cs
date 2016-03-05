using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Dnx;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroToolchains
    {
        /// <summary>
        /// there is no need to specify toolchain in explicit way
        /// depending on your target .Net framework compiler will pick up the right BenchmarkDotNet.dll
        ///   * for .NET 4.0 - 4.6 the one with Classic toolchain as Default
        ///   * for dnx451 the one with Dnx toolchain as Default
        /// </summary>
        public class IntroDefaultToolchain
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine(Toolchain.Current.Name);
            }
        }

        private class DnxToolchainConfig : ManualConfig
        {
            public DnxToolchainConfig()
            {
                Add(Job.Dry.With(DnxToolchain.Instance).With(Jit.Host));
            }
        }

        [Config(typeof(DnxToolchainConfig))]
        public class IntroExplicitToolchain
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine("DNX451");
            }
        }
    }
}