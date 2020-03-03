﻿using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
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

        [PublicAPI]
        public Jit Jit => RepresentativeBenchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, Resolver);

        public bool IsCoreRT => Runtime is CoreRtRuntime
            // given job can have CoreRT toolchain set, but Runtime == default ;)
            || (RepresentativeBenchmarkCase.Job.Infrastructure.TryGetToolchain(out var toolchain) && toolchain is CoreRtToolchain);

        public bool IsNetFramework => Runtime is ClrRuntime
            || (RepresentativeBenchmarkCase.Job.Infrastructure.TryGetToolchain(out var toolchain) && (toolchain is RoslynToolchain || toolchain is CsProjClassicNetToolchain));

        public Runtime Runtime => RepresentativeBenchmarkCase.Job.Environment.HasValue(EnvironmentMode.RuntimeCharacteristic)
                ? RepresentativeBenchmarkCase.Job.Environment.Runtime
                : RuntimeInformation.GetCurrentRuntime();

        public bool IsCustomBuildConfiguration => BuildConfiguration != InfrastructureMode.ReleaseConfigurationName;

        public override string ToString() => RepresentativeBenchmarkCase.Job.DisplayInfo;
    }
}