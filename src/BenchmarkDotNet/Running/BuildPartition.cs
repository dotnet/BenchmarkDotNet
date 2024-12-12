using System;
using System.IO;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Toolchains.Roslyn;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    public class BuildPartition
    {
        // We use an auto-increment global counter instead of Guid to guarantee uniqueness per benchmark run (Guid has a small chance to collide),
        // assuming there are fewer than 4 billion build partitions (a safe assumption).
        internal static int s_partitionCounter;

        public BuildPartition(BenchmarkBuildInfo[] benchmarks, IResolver resolver)
        {
            Resolver = resolver;
            RepresentativeBenchmarkCase = benchmarks[0].BenchmarkCase;
            Benchmarks = benchmarks;
            // Combine the benchmark's assembly name, folder info, and build partition id.
            string benchmarkAssemblyName = RepresentativeBenchmarkCase.Descriptor.Type.Assembly.GetName().Name;
            string folderInfo = RepresentativeBenchmarkCase.Job.FolderInfo;
            int id = Interlocked.Increment(ref s_partitionCounter);
            ProgramName = $"{benchmarkAssemblyName}-{folderInfo}-{id}";
            LogBuildOutput = benchmarks[0].Config.Options.IsSet(ConfigOptions.LogBuildOutput);
            GenerateMSBuildBinLog = benchmarks[0].Config.Options.IsSet(ConfigOptions.GenerateMSBuildBinLog);
        }

        public BenchmarkBuildInfo[] Benchmarks { get; }

        public string ProgramName { get; }

        /// <summary>
        /// the benchmarks are grouped by the build settings
        /// so you can use this benchmark to get the runtime settings
        /// </summary>
        public BenchmarkCase RepresentativeBenchmarkCase { get; }

        public IResolver Resolver { get; }

        public string AssemblyLocation => GetResolvedAssemblyLocation(RepresentativeBenchmarkCase.Descriptor.Type.Assembly);

        public string BuildConfiguration => RepresentativeBenchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, Resolver);

        public Platform Platform => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.PlatformCharacteristic, Resolver);

        [PublicAPI]
        public Jit Jit => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, Resolver);

        public bool IsNativeAot => RepresentativeBenchmarkCase.Job.IsNativeAOT();

        public bool IsWasm => Runtime is WasmRuntime // given job can have Wasm toolchain set, but Runtime == default ;)
            || (RepresentativeBenchmarkCase.Job.Infrastructure.TryGetToolchain(out var toolchain) && toolchain is WasmToolchain);

        public bool IsNetFramework => Runtime is ClrRuntime
            || (RepresentativeBenchmarkCase.Job.Infrastructure.TryGetToolchain(out var toolchain) && (toolchain is RoslynToolchain || toolchain is CsProjClassicNetToolchain));

        public Runtime Runtime => RepresentativeBenchmarkCase.Job.Environment.GetRuntime();

        public bool IsCustomBuildConfiguration => BuildConfiguration != InfrastructureMode.ReleaseConfigurationName;

        public TimeSpan Timeout => IsNativeAot && RepresentativeBenchmarkCase.Config.BuildTimeout == DefaultConfig.Instance.BuildTimeout
            ? TimeSpan.FromMinutes(5) // downloading all NativeAOT dependencies can take a LOT of time
            : RepresentativeBenchmarkCase.Config.BuildTimeout;

        public bool LogBuildOutput { get; }

        public bool GenerateMSBuildBinLog { get; }

        public override string ToString() => RepresentativeBenchmarkCase.Job.DisplayInfo;

        private static string GetResolvedAssemblyLocation(Assembly assembly) =>
            // in case of SingleFile, location.Length returns 0, so we use GetName() and
            // manually construct the path.
            assembly.Location.Length == 0 ? Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name) : assembly.Location;

        internal bool ForcedNoDependenciesForIntegrationTests
        {
            get
            {
                if (!XUnitHelper.IsIntegrationTest.Value || !RuntimeInformation.IsNetCore)
                    return false;

                var job = RepresentativeBenchmarkCase.Job;
                if (job.GetToolchain().Builder is not DotNetCliBuilder)
                    return false;

                return !job.HasDynamicBuildCharacteristic();
            }
        }
    }
}