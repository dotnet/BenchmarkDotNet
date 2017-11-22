using System.Reflection;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.Threading;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;
using System.Linq;
using System;

namespace BenchmarkDotNet.Samples
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Sleeps>(new AllowNonOptimized());
            Console.ReadLine();
        }
    }

    public class AllowNonOptimized : ManualConfig
    {
        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }

    [ShortRunJob]
    public class Sleeps
    {
        [Benchmark(Baseline = true)]
        public void Benchmarlow() => Thread.Sleep(700);

        [Benchmark]
        public void Benchmarast() => Thread.Sleep(5);
    }
}