using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    public class BuildPartition
    {
        public BuildPartition(BenchmarkBuildInfo[] benchmarks, IResolver resolver)
        {
            Resolver = resolver;
            RepresentativeBenchmarkCase = benchmarks[0].BenchmarkCase;
            Benchmarks = benchmarks;
            ProgramName = benchmarks[0].Config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles) ? RepresentativeBenchmarkCase.Job.FolderInfo : Guid.NewGuid().ToString();
        }

        public BenchmarkBuildInfo[] Benchmarks { get; }

        public string ProgramName { get; }

        /// <summary>
        /// the benchmarks are grouped by the build settings
        /// so you can use this benchmark to get the runtime settings
        /// </summary>
        public BenchmarkCase RepresentativeBenchmarkCase { get; }

        public IResolver Resolver { get; }

        public string AssemblyLocation => RepresentativeBenchmarkCase.Descriptor.Type.Assembly.Location;

        public string BuildConfiguration => RepresentativeBenchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, Resolver);

        public Platform Platform => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.PlatformCharacteristic, Resolver);

        public bool CanBuildInParallel => RepresentativeBenchmarkCase.Job.GetToolchain().CanBuildInParallel;

        public bool IsScenario => Benchmarks.Length == 1 && Benchmarks.All(benchmark => benchmark.BenchmarkCase.Descriptor.Kind == BenchmarkKind.Scenario);

        [PublicAPI]
        public Jit Jit => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, Resolver);

        public bool IsAOT => RepresentativeBenchmarkCase.Job.GetToolchain().IsAOT;

        public Runtime Runtime => RepresentativeBenchmarkCase.Job.Environment.HasValue(EnvironmentMode.RuntimeCharacteristic)
                ? RepresentativeBenchmarkCase.Job.Environment.Runtime
                : RuntimeInformation.GetCurrentRuntime();

        public bool IsCustomBuildConfiguration => BuildConfiguration != InfrastructureMode.ReleaseConfigurationName;

        public override string ToString() => RepresentativeBenchmarkCase.Job.DisplayInfo;
    }
}