using System;
using System.IO;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Roslyn;
using JetBrains.Annotations;

#nullable enable

namespace BenchmarkDotNet.Running
{
    public class BuildPartition
    {
        // We use an auto-increment global counter instead of Guid to guarantee uniqueness per benchmark run (Guid has a small chance to collide),
        // assuming there are fewer than 4 billion build partitions (a safe assumption).
        internal static int s_partitionCounter;

        internal static readonly BuildPartition Empty = new();

        public BuildPartition(BenchmarkBuildInfo[] benchmarks, IResolver resolver)
        {
            Resolver = resolver;
            RepresentativeBenchmarkCase = benchmarks[0].BenchmarkCase;
            Benchmarks = benchmarks;
            ProgramName = GetProgramName(RepresentativeBenchmarkCase, Interlocked.Increment(ref s_partitionCounter));
            LogBuildOutput = benchmarks[0].Config.Options.IsSet(ConfigOptions.LogBuildOutput);
            GenerateMSBuildBinLog = benchmarks[0].Config.Options.IsSet(ConfigOptions.GenerateMSBuildBinLog);
        }

        private BuildPartition()
        {
            Resolver = default!;
            RepresentativeBenchmarkCase = default!;
            Benchmarks = [];
            ProgramName = default!;
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

        public string BuildConfiguration => RepresentativeBenchmarkCase.Job.ResolveValue(InfrastructureMode.BuildConfigurationCharacteristic, Resolver)!;

        public Platform Platform => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.PlatformCharacteristic, Resolver);

        [PublicAPI]
        public Jit Jit => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, Resolver);

        public bool IsNativeAot => RepresentativeBenchmarkCase.Job.IsNativeAOT();

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
            assembly.Location.Length == 0 ? Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name!) : assembly.Location;

        internal static string GetProgramName(BenchmarkCase representativeBenchmarkCase, int id)
        {
            // Combine the benchmark's assembly name, folder info, and build partition id.
            string benchmarkAssemblyName = representativeBenchmarkCase.Descriptor.Type.Assembly.GetName().Name!;
            string folderInfo = representativeBenchmarkCase.Job.FolderInfo;
            var programName = $"{benchmarkAssemblyName}-{folderInfo}-{id}";
            // Very long program name can cause the path to exceed Windows' 260 character limit,
            // for example BenchmarkDotNet.IntegrationTests.ManualRunning.MultipleFrameworks.
            // 36 is an arbitrary limit, but it's the length of Guid strings which is what was used previously.
            const int MaxLength = 36;
            if (!OsDetector.IsWindows() || programName.Length <= MaxLength)
            {
                return programName;
            }
            programName = $"{benchmarkAssemblyName}-{id}";
            if (programName.Length <= MaxLength)
            {
                return programName;
            }
            programName = $"{folderInfo}-{id}";
            if (programName.Length <= MaxLength)
            {
                return programName;
            }
            return id.ToString();
        }

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
