using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Toolchains.Roslyn;
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
    }
}