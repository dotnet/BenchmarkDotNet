using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroRuntimes
    {
        private const string AvoidParsingException = "//";

        /// <summary>
        /// when Runtime is not set, then the default runtime is used - Host
        /// depending on your target .Net framework compiler will pick up the right BenchmarkDotNet.dll
        ///   * for .NET 4.0 - 4.6.2 the one with Clr as Default or Mono if this is Mono
        ///   * for .NET Core the one with Core as Default
        /// </summary>
        public class IntroDefaultToolchain
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine($"{AvoidParsingException} {RuntimeInformation.GetCurrentRuntime().GetToolchain()}");
            }
        }

        /// <summary>
        /// if your project is targeting multiple frameworks (net40, netcoreapp1.0)
        /// and you have dotnet cli installed
        /// you can target all runtimes that you support with single config
        /// </summary>
        private class MultipleRuntimesConfig : ManualConfig
        {
            public MultipleRuntimesConfig()
            {
                Add(new Job(Job.Dry, EnvMode.Clr, EnvMode.RyuJitX64));
                Add(new Job(Job.Dry, EnvMode.Core, EnvMode.RyuJitX64));
                Add(new Job(Job.Dry, EnvMode.Mono));
            }
        }

        [Config(typeof(MultipleRuntimesConfig))]
        public class IntroMultipleRuntimes
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine($"{AvoidParsingException} {RuntimeInformation.GetCurrentRuntime().GetToolchain()}");
            }
        }
    }
}