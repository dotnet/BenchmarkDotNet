using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    public class BuildPartition
    {
        public BuildPartition(BenchmarkBuildInfo[] benchmarks, IResolver resolver)
        {
            Resolver = resolver;
            RepresentativeBenchmark = benchmarks[0].Benchmark;
            Benchmarks = benchmarks;
            ProgramName = benchmarks[0].Config.KeepBenchmarkFiles ? RepresentativeBenchmark.Job.FolderInfo : Guid.NewGuid().ToString();
        }

        public BenchmarkBuildInfo[] Benchmarks { get; }

        public string ProgramName { get; }

        /// <summary>
        /// the benchmarks are groupped by the build settings
        /// so you can use this benchmark to get the runtime settings
        /// </summary>
        public Benchmark RepresentativeBenchmark { get; }

        public IResolver Resolver { get; }

        public string AssemblyLocation => RepresentativeBenchmark.Target.Type.Assembly.Location;

        public string BuildConfiguration => RepresentativeBenchmark.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, Resolver);

        public Platform Platform => RepresentativeBenchmark.Job.ResolveValue(EnvMode.PlatformCharacteristic, Resolver);

        public Jit Jit => RepresentativeBenchmark.Job.ResolveValue(EnvMode.JitCharacteristic, Resolver);

        public Runtime Runtime => RepresentativeBenchmark.Job.Env.HasValue(EnvMode.RuntimeCharacteristic)
                ? RepresentativeBenchmark.Job.Env.Runtime
                : RuntimeInformation.GetCurrentRuntime();

        public override string ToString() => RepresentativeBenchmark.Job.DisplayInfo;
    }
}