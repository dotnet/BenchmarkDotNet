using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Running
{
    public class BuildPartition
    {
        public BuildPartition(BenchmarkBuildInfo[] benchmarks, IResolver resolver)
        {
            Resolver = resolver;
            RepresentativeBenchmark = benchmarks.Select(info => info.Benchmark).FirstOrDefault();
            Benchmarks = benchmarks;
            ProgramName = Guid.NewGuid().ToString(); // todo: figure out some nice name (first job.Folder?)
        }

        public BenchmarkBuildInfo[] Benchmarks { get; }

        public string ProgramName { get; }

        /// <summary>
        /// the benchmarks are groupped by the build settings
        /// so you can use this benchmark to get the settings
        /// </summary>
        public Benchmark RepresentativeBenchmark { get; }

        public IResolver Resolver { get; }

        public string AssemblyLocation => RepresentativeBenchmark.Target.Type.Assembly.Location;

        public string BuildConfiguration => RepresentativeBenchmark.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, Resolver);

        public Platform Platform => RepresentativeBenchmark.Job.ResolveValue(EnvMode.PlatformCharacteristic, Resolver);

        public Jit Jit => RepresentativeBenchmark.Job.ResolveValue(EnvMode.JitCharacteristic, Resolver);

        private Runtime Runtime => RepresentativeBenchmark.Job.Env.HasValue(EnvMode.RuntimeCharacteristic)
                ? RepresentativeBenchmark.Job.Env.Runtime
                : RuntimeInformation.GetCurrentRuntime();

        public override string ToString() => $"{Runtime}-{Platform}-{Jit}-{RepresentativeBenchmark.Job.GetToolchain()}";
    }
}