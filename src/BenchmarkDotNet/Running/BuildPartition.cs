using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;

namespace BenchmarkDotNet.Running
{
    public class BuildPartition
    {
        public BuildPartition(BenchmarkBuildInfo[] benchmarks, IResolver resolver)
        {
            Resolver = resolver;
            RepresentativeBenchmarkCase = benchmarks[0].BenchmarkCase;
            Benchmarks = benchmarks;
            ProgramName = benchmarks[0].Config.KeepBenchmarkFiles ? RepresentativeBenchmarkCase.Job.FolderInfo : Guid.NewGuid().ToString();
        }

        public BenchmarkBuildInfo[] Benchmarks { get; }

        public string ProgramName { get; }

        /// <summary>
        /// the benchmarks are groupped by the build settings
        /// so you can use this benchmark to get the runtime settings
        /// </summary>
        public BenchmarkCase RepresentativeBenchmarkCase { get; }

        public IResolver Resolver { get; }

        public string AssemblyLocation => RepresentativeBenchmarkCase.Descriptor.Type.Assembly.Location;

        public string BuildConfiguration => RepresentativeBenchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, Resolver);

        public Platform Platform => RepresentativeBenchmarkCase.Job.ResolveValue(EnvMode.PlatformCharacteristic, Resolver);

        public Jit Jit => RepresentativeBenchmarkCase.Job.ResolveValue(EnvMode.JitCharacteristic, Resolver);

        public bool IsCoreRT => Runtime is CoreRtRuntime
            || (RepresentativeBenchmarkCase.Job.Infrastructure.HasValue(InfrastructureMode.ToolchainCharacteristic) && RepresentativeBenchmarkCase.Job.Infrastructure.Toolchain is CoreRtToolchain); // given job can have CoreRT toolchain set, but Runtime == default ;)

        private Runtime Runtime => RepresentativeBenchmarkCase.Job.Env.HasValue(EnvMode.RuntimeCharacteristic)
                ? RepresentativeBenchmarkCase.Job.Env.Runtime
                : RuntimeInformation.GetCurrentRuntime();

        public override string ToString() => RepresentativeBenchmarkCase.Job.DisplayInfo;
    }
}