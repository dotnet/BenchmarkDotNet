using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

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
    }
}