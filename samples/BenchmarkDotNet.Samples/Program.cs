using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Loggers;
using System.Threading;

namespace BenchmarkDotNet.Samples
{
    /*public class Program
    {
        public static void Main(string[] args)
        {
            IToolchain toolChain = WasmToolChain.NetCoreApp50Wasm;

            ManualConfig modifiedConfig = DefaultConfig.Instance.AddJob(Job.Default.WithToolchain(toolChain).AsDefault());
            // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, modifiedConfig);
        }
    }*/

    public class Program
    {
        public static void Main(string[] args)
        {
            var toolChain = WasmToolChain.NetCoreApp50Wasm;
            var job = Job.Dry.WithToolchain(toolChain).AsDefault(); ; // Job.Dry == runs the benchmark only once, great for testing

            BenchmarkRunner.Run<Simplest>(ManualConfig.CreateEmpty().AddJob(job).AddLogger(ConsoleLogger.Default)); // runner does not support filtering, it runs benchmarks from a single class
        }
    }
    public class Simplest
    {
        [Benchmark] public void DoNothing() { Thread.Sleep(1000); }
    }
}